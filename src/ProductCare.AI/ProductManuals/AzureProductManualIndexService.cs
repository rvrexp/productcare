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

        if (await IsIndexedAsync(productId, cancellationToken))
        {
            return new ProductManualStatus(ProductManualState.Ready, false, null);
        }

        BlobProperties blobProperties = (await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;
        SearchIndexerStatus indexerStatus = (await _indexerClient.GetIndexerStatusAsync(_options.IndexerName, cancellationToken)).Value;
        IndexerExecutionResult? lastResult = indexerStatus.LastResult;

        if (indexerStatus.Status == IndexerStatus.Error)
        {
            return new ProductManualStatus(ProductManualState.Failed, false, null);
        }

        if (lastResult is null)
        {
            await StartIndexerAsync(productId, cancellationToken);
            return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
        }

        if (lastResult.Status == IndexerExecutionStatus.InProgress)
        {
            return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
        }

        if (!lastResult.StartTime.HasValue)
        {
            await StartIndexerAsync(productId, cancellationToken);
            return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
        }

        if (lastResult.StartTime.Value < blobProperties.LastModified)
        {
            await StartIndexerAsync(productId, cancellationToken);
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

    public async Task<ProductManualStatus> UploadAsync(Guid productId, string manufacturer, string modelNumber, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturer);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelNumber);
        ArgumentNullException.ThrowIfNull(content);

        BlobClient blobClient = GetBlobClient(productId);
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
                ["productid"] = productId.ToString("D")
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

        await StartIndexerAsync(productId, cancellationToken);

        return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
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
            throw new ConflictException("The product manual is not currently in a failed indexing state.");
        }

        if (!currentStatus.CanRetry)
        {
            throw new ConflictException(currentStatus.RetryAfterUtc.HasValue
                ? $"The indexing retry is not available until {currentStatus.RetryAfterUtc.Value:O}."
                : "This indexing failure requires administrator attention before it can be retried.");
        }

        await StartIndexerAsync(productId, cancellationToken);

        return new ProductManualStatus(ProductManualState.PendingIndexing, false, null);
    }

    private async Task StartIndexerAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            await _indexerClient.RunIndexerAsync(_options.IndexerName, cancellationToken);

            _logger.LogInformation("Started product manual indexing for product {ProductId}.", productId);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            _logger.LogInformation("Product manual indexer was already running when indexing was requested for product {ProductId}.", productId);
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

    private BlobClient GetBlobClient(Guid productId) =>
        _containerClient.GetBlobClient($"uploads/{productId:N}/manual.pdf");
}