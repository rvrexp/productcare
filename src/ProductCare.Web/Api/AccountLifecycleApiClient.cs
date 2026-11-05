using System.Net;
using System.Net.Http.Json;

namespace ProductCare.Web.Api;

public sealed class AccountLifecycleApiClient
{
    private readonly HttpClient _httpClient;

    public AccountLifecycleApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"api/v1/products/{productId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteMyAccountAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync("api/v1/account/me", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<AdminUserResponse>> GetAdminUsersAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/admin/users", cancellationToken);
        response.EnsureSuccessStatusCode();

        List<AdminUserResponse>? users = await response.Content.ReadFromJsonAsync<List<AdminUserResponse>>(cancellationToken: cancellationToken);
        return users ?? [];
    }

    public async Task<AdminUserDetailsResponse?> GetAdminUserAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"api/v1/admin/users/{accountId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AdminUserDetailsResponse>(cancellationToken: cancellationToken);
    }

    public Task<AdminUserResponse?> SuspendAdminUserAsync(Guid accountId, CancellationToken cancellationToken = default) => PostAdminActionAsync(accountId, "suspend", cancellationToken);

    public Task<AdminUserResponse?> RestoreAdminUserAsync(Guid accountId, CancellationToken cancellationToken = default) => PostAdminActionAsync(accountId, "restore", cancellationToken);

    public async Task PurgeAdminUserAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsync($"api/v1/admin/users/{accountId}/purge", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAdminUserAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"api/v1/admin/users/{accountId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<AdminUserResponse?> PostAdminActionAsync(Guid accountId, string action, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.PostAsync($"api/v1/admin/users/{accountId}/{action}", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken: cancellationToken);
    }
}
