using ProductCare.Domain.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.Application.Access
{
    public interface IUserAccessRepository
    {
        Task<UserAccount?> GetAccountAsync(UserIdentity identity, CancellationToken cancellationToken = default);

        Task<UserAccount?> GetAccountAsync(Guid userAccountId, CancellationToken cancellationToken = default);

        Task<AccessRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

        Task<bool> HasPendingRequestAsync(Guid userAccountId, CancellationToken cancellationToken = default);

        Task<DateTime?> GetLatestRequestDateAsync(Guid userAccountId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AccessRequestRecord>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

        void Add(UserAccount account);

        void Add(AccessRequest request);

        Task UpdateAsync(UserAccount account, CancellationToken cancellationToken = default);

        Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}