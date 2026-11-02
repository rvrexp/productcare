using ProductCare.Domain.Access;

namespace ProductCare.Application.Administration;

public sealed class UserAdministrationService : IUserAdministrationService
{
    private readonly IUserAdministrationRepository _repository;

    public UserAdministrationService(IUserAdministrationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<IReadOnlyList<AdminUserRecord>> ListAsync(CancellationToken cancellationToken = default) => _repository.ListAsync(cancellationToken);

    public Task<AdminUserRecord?> GetAsync(Guid accountId, CancellationToken cancellationToken = default) => _repository.GetAsync(accountId, cancellationToken);

    public async Task<AdminUserRecord?> SuspendAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        AdminUserRecord? user = await _repository.GetAsync(accountId, cancellationToken);

        if (user is null || user.Status == UserAccountStatus.Disabled)
        {
            return user;
        }

        return await _repository.SetStatusAsync(accountId, UserAccountStatus.Disabled, DateTime.UtcNow, cancellationToken);
    }

    public async Task<AdminUserRecord?> RestoreAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        AdminUserRecord? user = await _repository.GetAsync(accountId, cancellationToken);

        if (user is null || user.Status != UserAccountStatus.Disabled)
        {
            return user;
        }

        UserAccountStatus restoredStatus = user.AccessExpiresUtc.HasValue && user.AccessExpiresUtc.Value <= DateTime.UtcNow
            ? UserAccountStatus.Expired
            : UserAccountStatus.Active;

        return await _repository.SetStatusAsync(accountId, restoredStatus, DateTime.UtcNow, cancellationToken);
    }
}
