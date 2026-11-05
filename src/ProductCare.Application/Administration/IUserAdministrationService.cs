namespace ProductCare.Application.Administration;

public interface IUserAdministrationService
{
    Task<IReadOnlyList<AdminUserRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task<AdminUserRecord?> GetAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<AdminUserDetailsRecord?> GetDetailsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<AdminUserRecord?> SuspendAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<AdminUserRecord?> RestoreAsync(Guid accountId, CancellationToken cancellationToken = default);
}
