using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ProductCare.Web.Api;

/// <summary>
/// Wraps calls from the Blazor client to the ProductCare backend API.
/// </summary>
public sealed class ProductCareApiClient
{
    private const long MaxImageFileSize = 5 * 1024 * 1024;

    private readonly HttpClient _httpClient;

    public ProductCareApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Retrieves the current backend health status used by the UI.
    /// </summary>
    public async Task<SystemStatusResponse?> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/system/status", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SystemStatusResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Uploads a product image for AI-based product identification.
    /// </summary>
    public async Task<ProductIdentificationResponse?> IdentifyProductAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using Stream fileStream = file.OpenReadStream(MaxImageFileSize);
        using StreamContent fileContent = new(fileStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        using MultipartFormDataContent form = new();
        form.Add(fileContent, "image", file.Name);

        using HttpResponseMessage response = await _httpClient.PostAsync("api/v1/products/identify", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductIdentificationResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates a new product record for the authenticated user.
    /// </summary>
    public async Task<ProductResponse?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/v1/products", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves the products saved for the current user.
    /// </summary>
    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/products", cancellationToken);
        response.EnsureSuccessStatusCode();

        List<ProductResponse>? products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(cancellationToken: cancellationToken);

        return products ?? [];
    }

    /// <summary>
    /// Retrieves a single saved product by its identifier.
    /// </summary>
    public async Task<ProductResponse?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"api/v1/products/{productId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Submits a grounded product-manual question for the selected product.
    /// </summary>
    public async Task<ProductKnowledgeResponse?> AskProductQuestionAsync(Guid productId, string question, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        AskProductQuestionRequest request = new(question);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/v1/products/{productId}/questions", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductKnowledgeResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retrieves current product safety and recall information.
    /// </summary>
    public async Task<ProductSafetyResponse?> GetProductSafetyAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"api/v1/products/{productId}/safety", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductSafetyResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a conversational assistant message for the selected product.
    /// </summary>
    public async Task<ProductAssistantResponse?> AskProductAssistantAsync(Guid productId, string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        AskProductAssistantRequest request = new(message);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/v1/products/{productId}/assistant", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductAssistantResponse>(cancellationToken: cancellationToken);
    }

    private sealed record AskProductQuestionRequest(string Question);

    private sealed record AskProductAssistantRequest(string Message);
}

/// <summary>
/// Represents the backend system status returned to the client.
/// </summary>
public sealed record SystemStatusResponse(string Status);