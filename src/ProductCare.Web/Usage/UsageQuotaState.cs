using Microsoft.AspNetCore.Components.Authorization;
using ProductCare.Web.Api;

namespace ProductCare.Web.Usage;

public sealed class UsageQuotaState : IDisposable
{
    private readonly ProductCareApiClient _apiClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public UsageQuotaState(ProductCareApiClient apiClient, AuthenticationStateProvider authenticationStateProvider)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));

        _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public event Action? Changed;

    public UsageQuotaSummaryResponse? Summary { get; private set; }

    public bool IsLoaded { get; private set; }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoaded)
        {
            return;
        }

        await RefreshAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        AuthenticationState authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();

        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            Reset();
            return;
        }

        Summary = await _apiClient.GetUsageQuotaSummaryAsync(cancellationToken);
        IsLoaded = Summary is not null;

        Changed?.Invoke();
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        Reset();
    }

    private void Reset()
    {
        Summary = null;
        IsLoaded = false;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}