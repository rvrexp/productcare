using Microsoft.AspNetCore.Components.Authorization;
using ProductCare.Web.Api;

namespace ProductCare.Web.Access;

public sealed class UserAccessState : IDisposable
{
    private readonly ProductCareApiClient _apiClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public UserAccessState(ProductCareApiClient apiClient, AuthenticationStateProvider authenticationStateProvider)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));

        _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public event Action? Changed;

    public UserAccessResponse? Access { get; private set; }

    public bool IsLoaded { get; private set; }

    public bool IsActive => string.Equals(Access?.Status, "Active", StringComparison.OrdinalIgnoreCase);

    public bool IsPending => string.Equals(Access?.Status, "PendingApproval", StringComparison.OrdinalIgnoreCase);

    public bool IsAdmin => Access?.IsAdmin == true;
    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        AuthenticationState authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();

        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            Access = null;
            IsLoaded = false;
            Changed?.Invoke();
            return;
        }

        if (IsLoaded)
        {
            return;
        }

        await LoadAuthenticatedAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        AuthenticationState authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();

        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            Access = null;
            IsLoaded = false;
            Changed?.Invoke();
            return;
        }

        await LoadAuthenticatedAsync(cancellationToken);
    }

    private async Task LoadAuthenticatedAsync(CancellationToken cancellationToken)
    {
        Access = await _apiClient.GetUserAccessAsync(cancellationToken);
        IsLoaded = true;
        Changed?.Invoke();
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        Access = null;
        IsLoaded = false;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}