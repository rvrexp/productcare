using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using ProductCare.Application.Accounts;
using ProductCare.Application.ProductAssistant;

namespace ProductCare.AI.ProductLifecycle;

public sealed class AzureProductResourceCleanupService : IProductResourceCleanupService
{
    private const string ChunkIdField = "chunk_id";

    private readonly BlobContainerClient _containerClient;
    private readonly SearchClient _searchClient;
    private readonly IProductAssistantService _productAssistantService;
    private readonly ILogger<AzureProductResourceCleanupService> _logger;

    public AzureProductResourceCleanupService(
        BlobContainerClient containerClient,
        SearchClient searchClient,
        IProductAssistantService productAssistantService,
        ILogger<AzureProductResourceCleanupService> logger)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _productAssistantService = productAssistantService ?? throw new ArgumentNullException(nameof(productAssistantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DeleteAsync(Guid productId, string? assistantConversationId, CancellationToken cancellationToken = default)
    {
        await DeleteSearchDocumentsAsync(productId, cancellationToken);

        BlobClient blobClient = _containerClient.GetBlobClient($"uploads/{productId:N}/manual.pdf");
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(assistantConversationId))
        {
            return;
        }

        try
        {
            await _productAssistantService.DeleteConversationAsync(assistantConversationId, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            _logger.LogInformation("The remote ProductCare assistant conversation for product {ProductId} was already absent.", productId);
        }
    }

    private async Task DeleteSearchDocumentsAsync(Guid productId, CancellationToken cancellationToken)
    {
        SearchOptions options = new()
        {
            Size = 1000,
            Filter = SearchFilter.Create($"productid eq {productId.ToString("D")}")
        };

        options.Select.Add(ChunkIdField);

        Response<SearchResults<SearchDocument>> response = await _searchClient.SearchAsync<SearchDocument>("*", options, cancellationToken);
        List<string> chunkIds = [];

        await foreach (SearchResult<SearchDocument> result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            if (result.Document.TryGetValue(ChunkIdField, out object? value) && value is not null)
            {
                string? chunkId = value.ToString();

                if (!string.IsNullOrWhiteSpace(chunkId))
                {
                    chunkIds.Add(chunkId);
                }
            }
        }

        if (chunkIds.Count == 0)
        {
            return;
        }

        await _searchClient.DeleteDocumentsAsync(ChunkIdField, chunkIds.Distinct(StringComparer.Ordinal), cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted {ChunkCount} indexed manual chunks for product {ProductId}.", chunkIds.Count, productId);
    }
}
