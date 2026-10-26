using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ProductCare.Web.Api;

public sealed class ProductManualApiClient
{
    private const long MaxManualFileSize = 15 * 1024 * 1024;

    private readonly HttpClient _httpClient;

    public ProductManualApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ProductManualStatusResponse?> GetStatusAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"api/v1/products/{productId}/manual", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductManualStatusResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ProductManualStatusResponse?> UploadAsync(Guid productId, IBrowserFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using Stream stream = file.OpenReadStream(MaxManualFileSize, cancellationToken);

        using StreamContent fileContent = new(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        using MultipartFormDataContent form = new();
        form.Add(fileContent, "file", file.Name);

        using HttpResponseMessage response = await _httpClient.PostAsync($"api/v1/products/{productId}/manual", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductManualStatusResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ProductManualStatusResponse?> RetryAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsync($"api/v1/products/{productId}/manual/retry", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductManualStatusResponse>(cancellationToken: cancellationToken);
    }
}

public sealed record ProductManualStatusResponse(string State, bool Uploaded, bool Ready, bool CanRetry, DateTime? RetryAfterUtc);