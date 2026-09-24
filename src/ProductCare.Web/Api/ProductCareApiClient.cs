using System.Net.Http.Json;

namespace ProductCare.Web.Api;

public sealed class ProductCareApiClient(HttpClient httpClient)
{
    public async Task<SystemStatusResponse?> GetSystemStatusAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            "api/v1/system/status",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<SystemStatusResponse>(
                cancellationToken);
    }
}

public sealed record SystemStatusResponse(
    string Status);