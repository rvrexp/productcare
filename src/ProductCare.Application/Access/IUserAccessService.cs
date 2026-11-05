using ProductCare.Domain.Identity;

namespace ProductCare.Application.Access
{
    public interface IUserAccessService
    {
        Task<UserAccessRecord> GetStatusAsync(UserIdentity identity, CancellationToken cancellationToken = default);

        Task<UserAccessRecord> GetOrRegisterStatusAsync(AccessUser user, CancellationToken cancellationToken = default);

        Task<UserAccessRecord> SubmitRequestAsync(AccessUser user, NewAccessRequest request, CancellationToken cancellationToken = default);

        Task<bool> HasActiveAccessAsync(UserIdentity identity, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AccessRequestRecord>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);

        Task<AccessRequestRecord?> ApproveAsync(Guid requestId, UserIdentity reviewer, DateTime accessExpiresUtc, CancellationToken cancellationToken = default);

        Task<AccessRequestRecord?> RejectAsync(Guid requestId, UserIdentity reviewer, CancellationToken cancellationToken = default);
    }
}