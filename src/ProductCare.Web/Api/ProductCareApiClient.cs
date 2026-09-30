using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;

namespace ProductCare.Web.Api;

public sealed class ProductCareApiClient(HttpClient httpClient)
{
    public async Task<SystemStatusResponse?> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/v1/system/status", cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SystemStatusResponse>(cancellationToken);
    }

    public async Task<ProductIdentificationResponse?> IdentifyProductAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        const long maxFileSize = 5 * 1024 * 1024;

        await using Stream fileStream = file.OpenReadStream(maxFileSize);

        using StreamContent fileContent = new(fileStream);

        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        using MultipartFormDataContent form = new();

        form.Add(fileContent, "image", file.Name);

        using HttpResponseMessage response = await httpClient.PostAsync("api/v1/products/identify", form, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductIdentificationResponse>(cancellationToken: cancellationToken);
    }
    public async Task<ProductResponse?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync( "api/v1/products", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync( "api/v1/products", cancellationToken);

        response.EnsureSuccessStatusCode();

        List<ProductResponse>? products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(cancellationToken: cancellationToken);

        return products ?? [];
    }
}

public sealed record SystemStatusResponse(
    string Status);