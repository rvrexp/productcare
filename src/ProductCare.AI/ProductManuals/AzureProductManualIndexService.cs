using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductCare.AI.Configuration;
using ProductCare.Application.Common.Exceptions;
using ProductCare.Application.ProductKnowledge;
using ProductCare.Application.ProductManuals;

namespace ProductCare.AI.ProductManuals;

public sealed class AzureProductManualIndexService : IProductManualIndexService
{
    private const string ChunkIdField = "chunk_id";
    private const string SearchSkipMetadataKey = "AzureSearch_Skip";
    private const string SecurityScanETagMetadataKey = "ProductCare_SecurityScanETag";
    private const string MalwareScanResultTagKey = "Malware scanning scan result";
    private const string MalwareCleanResult = "No threats found";
    private const string MalwareMaliciousResult = "Malicious";

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    private readonly BlobContainerClient _containerClient;
    private readonly SearchClient _searchClient;
    private readonly SearchIndexerClient _indexerClient;
    private readonly ProductManualIngestionOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AzureProductManualIndexService> _logger;

    public AzureProductManualIndexService(
        BlobContainerClient containerClient,
        SearchClient searchClient,
        SearchIndexerClient indexerClient,
        IOptions<ProductManualIngestionOptions> options,
        TimeProvider timeProvider,
        ILogger<AzureProductManualIndexService> logger)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _indexerClient = indexerClient ?? throw new ArgumentNullException(nameof(indexerClient));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductManualStatus> GetStatusAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = GetBlobClient(productId);
        Response<bool> exists = await blobClient.ExistsAsync(cancellationToken);

        if (!exists.Value)
        {
            return new ProductManualStatus(ProductManualState.NotUploaded, false, null);
        }

        BlobProperties blobProperties = (await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;

        if (IsSearchSkipped(blobProperties.Metadata))
        {
            ProductManualSecurityScanResult scanResult = await GetSecurityScanResultAsync(blobClient, productId, cancellationToken);

            return scanResult switch
            {
                ProductManualSecurityScanResult.Malicious => new ProductManualStatus(ProductManualState.SecurityRejected, false, null),
                ProductManualSecurityScanResult.Failed => new ProductManualStatus(ProductManualState.SecurityScanFailed, false, null),
                _ => new ProductManualStatus(ProductManualState.PendingSecurityScan, false, null)
            };
        }

        if (await IsIndexedAsync(productId, cancellationToken))
        {
            return new ProductManualStatus(ProductManualState.Ready, false, null);
        }

        return await GetIndexingStatusAsync(productId, blobProperties, cancellationToken);
    }

    public async Task<ProductManualStatus> UploadAsync(Guid productId, string manufacturer, string modelNumber, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturer);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelNumber);
        ArgumentNullException.ThrowIfNull(content);

        BlobClient blobClient = GetBlobClient(productId);

        if ((await blobClient.ExistsAsync(cancellationToken)).Value)
        {
            ProductManualStatus existingStatus = await GetStatusAsync(productId, cancellationToken);

            if (existingStatus.State is ProductManualState.SecurityRejected or ProductManualState.SecurityScanFailed)
            {
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            }
            else
            {
                throw new ConflictException("A product manual has already been uploaded for this product.");
            }
        }

        string productKey = ProductKnowledgeKey.Create(manufacturer, modelNumber);

        BlobUploadOptions uploadOptions = new()
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "application/pdf"
            },
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["productkey"] = productKey,
                ["productid"] = productId.ToString("D"),
                [SearchSkipMetadataKey] = "true"
            },
            Conditions = new BlobRequestConditions
            {
                IfNoneMatch = ETag.All
            }
        };

        try
        {
            await blobClient.UploadAsync(content, uploadOptions, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status is 409 or 412)
        {
            throw new ConflictException("A product manual has already been uploaded for this product.");
        }

        _logger.LogInformation("Uploaded a product manual for product {ProductId} in security-scan quarantine state.", productId);

        return new ProductManualStatus(ProductManualState.PendingSecurityScan, false, null);
    }

    public async Task<ProductManualStatus> RetryAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        ProductManualStatus currentStatus = await GetStatusAsync(productId, cancellationToken);

        if (currentStatus.State == ProductManualState.Ready)
        {
            return currentStatus;
        }

        if (currentStatus.State != ProductManualState.Failed)
        {
            throw new ConflictException("The product manual is not currently in a retryable indexing state.");
        }

        if (!currentStatus.CanRetry)
        {
            throw new ConflictException(currentStatus.RetryAfterUtc.HasValue
                ? $"The indexing retry is not available until {currentStatus.RetryAfterUtc.Value:O}."
                : "This indexing failure requires administrator attention before it can be retried.");
        }

        bool started = await TryStartIndexerAsync(productId, cancellationToken);

        if (!started)
        {
            throw new ConflictException("The product manual indexer is currently busy. Please try the retry action again shortly.");
        }

        return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
    }

    public async Task ProcessSecurityScanResultAsync(Uri blobUri, string blobETag, string scanResultType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blobUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobETag);
        ArgumentException.ThrowIfNullOrWhiteSpace(scanResultType);

        if (!TryGetProductId(blobUri, out Guid productId))
        {
            _logger.LogWarning("Ignored a malware scan event for a blob outside the ProductCare manual path.");
            return;
        }

        if (!scanResultType.Equals(MalwareCleanResult, StringComparison.OrdinalIgnoreCase))
        {
            if (scanResultType.Equals(MalwareMaliciousResult, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Defender for Storage rejected the uploaded manual for product {ProductId} as malicious.", productId);
            }
            else
            {
                _logger.LogWarning("Defender for Storage returned a non-clean scan result for product {ProductId}: {ScanResultType}.", productId, scanResultType);
            }

            return;
        }

        BlobClient blobClient = GetBlobClient(productId);
        BlobProperties properties;

        try
        {
            properties = (await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            _logger.LogWarning("Ignored a clean malware scan event because the ProductCare manual blob no longer exists for product {ProductId}.", productId);
            return;
        }

        string normalizedEventETag = NormalizeETag(blobETag);
        string? approvedETag = GetMetadataValue(properties.Metadata, SecurityScanETagMetadataKey);

        if (!string.IsNullOrWhiteSpace(approvedETag) && NormalizeETag(approvedETag).Equals(normalizedEventETag, StringComparison.Ordinal))
        {
            if (await IsIndexedAsync(productId, cancellationToken))
            {
                _logger.LogInformation("Ignored a duplicate clean malware scan event because the manual is already indexed for product {ProductId}.", productId);
                return;
            }

            await StartIndexerRequiredAsync(productId, cancellationToken);
            return;
        }

        string currentBlobETag = NormalizeETag(properties.ETag.ToString());

        if (!currentBlobETag.Equals(normalizedEventETag, StringComparison.Ordinal))
        {
            _logger.LogWarning("Ignored a stale malware scan event for product {ProductId} because the scanned blob ETag no longer matches the current blob.", productId);
            return;
        }

        ProductManualSecurityScanResult storedScanResult = await GetSecurityScanResultAsync(blobClient, productId, cancellationToken);

        if (storedScanResult == ProductManualSecurityScanResult.Pending)
        {
            throw new InvalidOperationException("The Defender for Storage scan event arrived before the blob scan-result tag became available.");
        }

        if (storedScanResult != ProductManualSecurityScanResult.Clean)
        {
            _logger.LogWarning("A clean malware scan event was not accepted because the blob scan-result tag is not clean for product {ProductId}.", productId);
            return;
        }

        await ReleaseForIndexingAsync(blobClient, productId, normalizedEventETag, properties, cancellationToken);
        await StartIndexerRequiredAsync(productId, cancellationToken);
    }

    private async Task<ProductManualStatus> GetIndexingStatusAsync(Guid productId, BlobProperties blobProperties, CancellationToken cancellationToken)
    {
        SearchIndexerStatus indexerStatus = (await _indexerClient.GetIndexerStatusAsync(_options.IndexerName, cancellationToken)).Value;
        IndexerExecutionResult? lastResult = indexerStatus.LastResult;

        if (indexerStatus.Status == IndexerStatus.Error)
        {
            return new ProductManualStatus(ProductManualState.Failed, false, null);
        }

        if (lastResult is null || lastResult.Status == IndexerExecutionStatus.InProgress || !lastResult.StartTime.HasValue)
        {
            return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
        }

        if (lastResult.StartTime.Value < blobProperties.LastModified)
        {
            return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
        }

        if (lastResult.Status == IndexerExecutionStatus.TransientFailure)
        {
            DateTime retryAfterUtc = (lastResult.EndTime ?? _timeProvider.GetUtcNow()).UtcDateTime.Add(RetryDelay);
            bool canRetry = _timeProvider.GetUtcNow().UtcDateTime >= retryAfterUtc;

            return new ProductManualStatus(ProductManualState.Failed, canRetry, retryAfterUtc);
        }

        if (lastResult.Status == IndexerExecutionStatus.Success && lastResult.EndTime.HasValue && lastResult.EndTime.Value >= blobProperties.LastModified)
        {
            _logger.LogWarning("Product manual indexer completed but no indexed chunk was found for product {ProductId}.", productId);
            return new ProductManualStatus(ProductManualState.Failed, false, null);
        }

        return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
    }

    private async Task<ProductManualSecurityScanResult> GetSecurityScanResultAsync(BlobClient blobClient, Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            Response<GetBlobTagResult> response = await blobClient.GetTagsAsync(cancellationToken: cancellationToken);
            string? scanResult = response.Value.Tags.Where(pair => pair.Key.Equals(MalwareScanResultTagKey, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Value).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(scanResult))
            {
                return ProductManualSecurityScanResult.Pending;
            }

            if (scanResult.Equals(MalwareCleanResult, StringComparison.OrdinalIgnoreCase))
            {
                return ProductManualSecurityScanResult.Clean;
            }

            if (scanResult.Equals(MalwareMaliciousResult, StringComparison.OrdinalIgnoreCase))
            {
                return ProductManualSecurityScanResult.Malicious;
            }

            _logger.LogWarning("Product manual security scan returned a non-clean result for product {ProductId}: {ScanResult}.", productId, scanResult);
            return ProductManualSecurityScanResult.Failed;
        }
        catch (RequestFailedException exception)
        {
            _logger.LogError(exception, "ProductCare could not read the Defender for Storage malware scan result for product {ProductId}.", productId);
            return ProductManualSecurityScanResult.Failed;
        }
    }

    private async Task ReleaseForIndexingAsync(BlobClient blobClient, Guid productId, string scannedETag, BlobProperties properties, CancellationToken cancellationToken)
    {
        Dictionary<string, string> metadata = new(properties.Metadata, StringComparer.OrdinalIgnoreCase);
        string? skipKey = metadata.Keys.FirstOrDefault(key => key.Equals(SearchSkipMetadataKey, StringComparison.OrdinalIgnoreCase));

        if (skipKey is not null)
        {
            metadata.Remove(skipKey);
        }

        string? existingSecurityETagKey = metadata.Keys.FirstOrDefault(key => key.Equals(SecurityScanETagMetadataKey, StringComparison.OrdinalIgnoreCase));

        if (existingSecurityETagKey is not null)
        {
            metadata.Remove(existingSecurityETagKey);
        }

        metadata[SecurityScanETagMetadataKey] = scannedETag;

        BlobRequestConditions conditions = new()
        {
            IfMatch = properties.ETag
        };

        try
        {
            await blobClient.SetMetadataAsync(metadata, conditions, cancellationToken);
            _logger.LogInformation("Released the clean product manual for product {ProductId} to Azure AI Search indexing.", productId);
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            throw new InvalidOperationException("The product manual changed while ProductCare was releasing it for indexing. The Event Grid event should be retried.", exception);
        }
    }

    private async Task StartIndexerRequiredAsync(Guid productId, CancellationToken cancellationToken)
    {
        bool started = await TryStartIndexerAsync(productId, cancellationToken);

        if (!started)
        {
            throw new InvalidOperationException("The Azure AI Search indexer is currently busy. The Event Grid event should be retried.");
        }
    }

    private async Task<bool> TryStartIndexerAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            await _indexerClient.RunIndexerAsync(_options.IndexerName, cancellationToken);
            _logger.LogInformation("Started product manual indexing for product {ProductId}.", productId);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            _logger.LogInformation("Product manual indexer was already running when indexing was requested for product {ProductId}.", productId);
            return false;
        }
    }

    private async Task<bool> IsIndexedAsync(Guid productId, CancellationToken cancellationToken)
    {
        SearchOptions options = new()
        {
            Size = 1,
            Filter = SearchFilter.Create($"productid eq {productId.ToString("D")}")
        };

        options.Select.Add(ChunkIdField);

        Response<SearchResults<SearchDocument>> response = await _searchClient.SearchAsync<SearchDocument>("*", options, cancellationToken);

        await foreach (SearchResult<SearchDocument> _ in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            return true;
        }

        return false;
    }

    private bool TryGetProductId(Uri blobUri, out Guid productId)
    {
        productId = Guid.Empty;

        if (!blobUri.IsAbsoluteUri || !blobUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!blobUri.Host.Equals(_containerClient.Uri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        BlobUriBuilder uriBuilder = new(blobUri);

        if (!uriBuilder.BlobContainerName.Equals(_containerClient.Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] pathParts = uriBuilder.BlobName.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return pathParts.Length == 3
            && pathParts[0].Equals("uploads", StringComparison.Ordinal)
            && pathParts[2].Equals("manual.pdf", StringComparison.Ordinal)
            && Guid.TryParseExact(pathParts[1], "N", out productId);
    }

    private BlobClient GetBlobClient(Guid productId) => _containerClient.GetBlobClient($"uploads/{productId:N}/manual.pdf");

    private static bool IsSearchSkipped(IDictionary<string, string> metadata) => metadata.Any(pair => pair.Key.Equals(SearchSkipMetadataKey, StringComparison.OrdinalIgnoreCase) && pair.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

    private static string? GetMetadataValue(IDictionary<string, string> metadata, string key) => metadata.Where(pair => pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Value).FirstOrDefault();

    private static string NormalizeETag(string value) => value.Trim().Trim('"');

    private enum ProductManualSecurityScanResult
    {
        Pending,
        Clean,
        Malicious,
        Failed
    }
}
