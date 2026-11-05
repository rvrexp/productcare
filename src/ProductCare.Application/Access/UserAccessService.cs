using ProductCare.Application.Common.Exceptions;
using ProductCare.Domain.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.Application.Access
{
    public sealed class UserAccessService : IUserAccessService
    {
        private readonly IUserAccessRepository _repository;
        private readonly TimeProvider _timeProvider;

        public UserAccessService(IUserAccessRepository repository, TimeProvider timeProvider)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<UserAccessRecord> GetStatusAsync(UserIdentity identity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(identity);

            DateTime now = GetUtcNow();

            UserAccount? account = await _repository.GetAccountAsync(identity, cancellationToken);

            if (account is null)
            {
                return new UserAccessRecord(
                    UserAccountId: null,
                    Status: UserAccountStatus.Registered,
                    Plan: null,
                    AccessExpiresUtc: null,
                    RequestedUtc: null);
            }

            DateTime? requestedUtc = await _repository.GetLatestRequestDateAsync(account.Id, cancellationToken);

            return ToUserAccessRecord(account, requestedUtc, now);
        }

        public async Task<UserAccessRecord> GetOrRegisterStatusAsync(AccessUser user, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(user.Identity);

            DateTime now = GetUtcNow();

            UserAccount? account = await _repository.GetAccountAsync(user.Identity, cancellationToken);

            if (account is null)
            {
                account = UserAccount.Create(user.Identity, user.Email, user.DisplayName, now);
                _repository.Add(account);
                await _repository.SaveChangesAsync(cancellationToken);
            }
            else if (account.UpdateProfile(user.Email, user.DisplayName, now))
            {
                await _repository.UpdateAsync(account, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
            }

            DateTime? requestedUtc = await _repository.GetLatestRequestDateAsync(account.Id, cancellationToken);

            return ToUserAccessRecord(account, requestedUtc, now);
        }

        public async Task<UserAccessRecord> SubmitRequestAsync(AccessUser user, NewAccessRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(user.Identity);
            ArgumentNullException.ThrowIfNull(request);

            DateTime now = GetUtcNow();

            UserAccount? account = await _repository.GetAccountAsync(user.Identity, cancellationToken);
            bool isNewAccount = account is null;

            if (account is null)
            {
                account = UserAccount.Create(user.Identity, user.Email, user.DisplayName, now);
            }
            else
            {
                account.UpdateProfile(user.Email, user.DisplayName, now);
            }

            if (account.HasActiveAccess(now))
            {
                throw new ConflictException("The account already has active ProductCare access.");
            }

            if (await _repository.HasPendingRequestAsync(account.Id, cancellationToken))
            {
                throw new ConflictException("An access request is already pending.");
            }

            try
            {
                account.MarkPendingApproval(now);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }

            AccessRequest accessRequest = AccessRequest.Create(
                account.Id,
                request.Organization,
                request.Purpose,
                request.Message,
                now);

            if (isNewAccount)
            {
                _repository.Add(account);
            }
            else
            {
                await _repository.UpdateAsync(account, cancellationToken);
            }

            _repository.Add(accessRequest);

            await _repository.SaveChangesAsync(cancellationToken);

            return ToUserAccessRecord(account, now, now);
        }

        public async Task<bool> HasActiveAccessAsync(UserIdentity identity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(identity);

            UserAccount? account = await _repository.GetAccountAsync(identity, cancellationToken);

            return account is not null && account.HasActiveAccess(GetUtcNow());
        }

        public Task<IReadOnlyList<AccessRequestRecord>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
        {
            return _repository.GetPendingRequestsAsync(cancellationToken);
        }

        public async Task<AccessRequestRecord?> ApproveAsync(Guid requestId, UserIdentity reviewer, DateTime accessExpiresUtc, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reviewer);

            AccessRequest? request = await _repository.GetRequestAsync(requestId, cancellationToken);

            if (request is null)
            {
                return null;
            }

            UserAccount account = await _repository.GetAccountAsync(request.UserAccountId, cancellationToken)
                ?? throw new InvalidOperationException("The account associated with the access request could not be found.");

            DateTime now = GetUtcNow();

            try
            {
                request.Approve(reviewer, now);
                account.ApproveDemoAccess(accessExpiresUtc, now);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }
            catch (ArgumentException exception)
            {
                throw new ValidationException(exception.Message, exception);
            }

            await _repository.UpdateAsync(request, cancellationToken);
            await _repository.UpdateAsync(account, cancellationToken);

            await _repository.SaveChangesAsync(cancellationToken);

            return ToAccessRequestRecord(request, account);
        }

        public async Task<AccessRequestRecord?> RejectAsync(Guid requestId, UserIdentity reviewer, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reviewer);

            AccessRequest? request = await _repository.GetRequestAsync(requestId, cancellationToken);

            if (request is null)
            {
                return null;
            }

            UserAccount account = await _repository.GetAccountAsync(request.UserAccountId, cancellationToken)
                ?? throw new InvalidOperationException("The account associated with the access request could not be found.");

            DateTime now = GetUtcNow();

            try
            {
                request.Reject(reviewer, now);
                account.RejectAccess(now);
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message, exception);
            }

            await _repository.UpdateAsync(request, cancellationToken);
            await _repository.UpdateAsync(account, cancellationToken);

            await _repository.SaveChangesAsync(cancellationToken);

            return ToAccessRequestRecord(request, account);
        }

        private DateTime GetUtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static UserAccessRecord ToUserAccessRecord(UserAccount account, DateTime? requestedUtc, DateTime utcNow) => new(
            account.Id,
            account.GetEffectiveStatus(utcNow),
            account.Plan,
            account.AccessExpiresUtc,
            requestedUtc);

        private static AccessRequestRecord ToAccessRequestRecord(AccessRequest request, UserAccount account) => new(
            request.Id,
            request.UserAccountId,
            account.Email,
            account.DisplayName,
            request.Organization,
            request.Purpose,
            request.Message,
            request.Status,
            request.RequestedUtc,
            request.ReviewedUtc);
    }
}