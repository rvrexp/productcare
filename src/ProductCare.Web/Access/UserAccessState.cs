using Microsoft.AspNetCore.Components.Authorization;
using ProductCare.Web.Api;

namespace ProductCare.Web.Access;

public sealed class UserAccessState : IDisposable
{
    private readonly ProductCareApiClient _apiClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public UserAccessState(ProductCareApiClient apiClient, AuthenticationStateProvider authenticationStateProvider)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));

        _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public event Action? Changed;

    public UserAccessResponse? Access { get; private set; }

    public bool IsLoaded { get; private set; }

    public bool IsLoading { get; private set; }

    public bool IsActive => IsLoaded && string.Equals(Access?.Status, "Active", StringComparison.OrdinalIgnoreCase);

    public bool IsPending => IsLoaded && string.Equals(Access?.Status, "PendingApproval", StringComparison.OrdinalIgnoreCase);

    public bool IsAdmin => IsLoaded && Access?.IsAdmin == true;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoaded)
        {
            return;
        }

        await _loadLock.WaitAsync(cancellationToken);

        try
        {
            if (IsLoaded)
            {
                return;
            }

            await LoadCurrentUserAsync(cancellationToken);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken);

        try
        {
            IsLoaded = false;
            await LoadCurrentUserAsync(cancellationToken);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadCurrentUserAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        Changed?.Invoke();

        try
        {
            AuthenticationState authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            if (authenticationState.User.Identity?.IsAuthenticated != true)
            {
                Access = null;
                IsLoaded = false;
                return;
            }

            Access = await _apiClient.GetUserAccessAsync(cancellationToken);
            IsLoaded = true;
        }
        finally
        {
            IsLoading = false;
            Changed?.Invoke();
        }
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        Access = null;
        IsLoaded = false;
        IsLoading = true;
        Changed?.Invoke();

        try
        {
            AuthenticationState authenticationState = await authenticationStateTask;

            if (authenticationState.User.Identity?.IsAuthenticated == true)
            {
                await RefreshAsync();
            }
            else
            {
                IsLoading = false;
                Changed?.Invoke();
            }
        }
        catch
        {
            IsLoading = false;
            Changed?.Invoke();
        }
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        _loadLock.Dispose();
    }
}