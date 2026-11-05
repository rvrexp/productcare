using ProductCare.Domain.Access;

namespace ProductCare.Application.Administration;

public interface IUserAdministrationRepository
{
    Task<IReadOnlyList<AdminUserRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task<AdminUserRecord?> GetAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<AdminUserDetailsRecord?> GetDetailsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<AdminUserRecord?> SetStatusAsync(Guid accountId, UserAccountStatus status, DateTime updatedUtc, CancellationToken cancellationToken = default);
}
