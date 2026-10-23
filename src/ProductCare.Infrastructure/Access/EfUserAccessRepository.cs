using Microsoft.EntityFrameworkCore;
using ProductCare.Application.Access;
using ProductCare.Domain.Access;
using ProductCare.Domain.Identity;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Access
{
    public sealed class EfUserAccessRepository : IUserAccessRepository
    {
        private readonly ProductCareDbContext _dbContext;

        public EfUserAccessRepository(ProductCareDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<UserAccount?> GetAccountAsync(UserIdentity identity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(identity);

            UserAccountEntity? entity = await _dbContext.UserAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(account => account.TenantId == identity.TenantId && account.UserId == identity.UserId, cancellationToken);

            return entity is null ? null : ToDomain(entity);
        }

        public async Task<UserAccount?> GetAccountAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            UserAccountEntity? entity = await _dbContext.UserAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(account => account.Id == userAccountId, cancellationToken);

            return entity is null ? null : ToDomain(entity);
        }

        public async Task<AccessRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            AccessRequestEntity? entity = await _dbContext.AccessRequests
                .AsNoTracking()
                .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);

            return entity is null ? null : ToDomain(entity);
        }

        public Task<bool> HasPendingRequestAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            return _dbContext.AccessRequests
                .AsNoTracking()
                .AnyAsync(request => request.UserAccountId == userAccountId && request.Status == AccessRequestStatus.Pending, cancellationToken);
        }

        public Task<DateTime?> GetLatestRequestDateAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            return _dbContext.AccessRequests
                .AsNoTracking()
                .Where(request => request.UserAccountId == userAccountId)
                .OrderByDescending(request => request.RequestedUtc)
                .Select(request => (DateTime?)request.RequestedUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AccessRequestRecord>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.AccessRequests
                .AsNoTracking()
                .Where(request => request.Status == AccessRequestStatus.Pending)
                .OrderBy(request => request.RequestedUtc)
                .Select(request => new AccessRequestRecord(
                    request.Id,
                    request.UserAccountId,
                    request.UserAccount.Email,
                    request.UserAccount.DisplayName,
                    request.Organization,
                    request.Purpose,
                    request.Message,
                    request.Status,
                    request.RequestedUtc,
                    request.ReviewedUtc))
                .ToListAsync(cancellationToken);
        }

        public void Add(UserAccount account)
        {
            ArgumentNullException.ThrowIfNull(account);

            _dbContext.UserAccounts.Add(ToEntity(account));
        }

        public void Add(AccessRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            _dbContext.AccessRequests.Add(ToEntity(request));
        }

        public async Task UpdateAsync(UserAccount account, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(account);

            UserAccountEntity entity = await _dbContext.UserAccounts
                .SingleOrDefaultAsync(existing => existing.Id == account.Id, cancellationToken)
                ?? throw new InvalidOperationException("User account could not be found.");

            entity.TenantId = account.Identity.TenantId;
            entity.UserId = account.Identity.UserId;
            entity.Email = account.Email;
            entity.DisplayName = account.DisplayName;
            entity.Status = account.Status;
            entity.Plan = account.Plan;
            entity.AccessExpiresUtc = account.AccessExpiresUtc;
            entity.UpdatedUtc = account.UpdatedUtc;
        }

        public async Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            AccessRequestEntity entity = await _dbContext.AccessRequests
                .SingleOrDefaultAsync(existing => existing.Id == request.Id, cancellationToken)
                ?? throw new InvalidOperationException("Access request could not be found.");

            entity.Status = request.Status;
            entity.ReviewedUtc = request.ReviewedUtc;
            entity.ReviewedByTenantId = request.ReviewedBy?.TenantId;
            entity.ReviewedByUserId = request.ReviewedBy?.UserId;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        private static UserAccountEntity ToEntity(UserAccount account) => new()
        {
            Id = account.Id,
            TenantId = account.Identity.TenantId,
            UserId = account.Identity.UserId,
            Email = account.Email,
            DisplayName = account.DisplayName,
            Status = account.Status,
            Plan = account.Plan,
            AccessExpiresUtc = account.AccessExpiresUtc,
            CreatedUtc = account.CreatedUtc,
            UpdatedUtc = account.UpdatedUtc
        };

        private static AccessRequestEntity ToEntity(AccessRequest request) => new()
        {
            Id = request.Id,
            UserAccountId = request.UserAccountId,
            Organization = request.Organization,
            Purpose = request.Purpose,
            Message = request.Message,
            Status = request.Status,
            RequestedUtc = request.RequestedUtc,
            ReviewedUtc = request.ReviewedUtc,
            ReviewedByTenantId = request.ReviewedBy?.TenantId,
            ReviewedByUserId = request.ReviewedBy?.UserId
        };

        private static UserAccount ToDomain(UserAccountEntity entity) => UserAccount.Restore(
            entity.Id,
            new UserIdentity(entity.TenantId, entity.UserId),
            entity.Email,
            entity.DisplayName,
            entity.Status,
            entity.Plan,
            entity.AccessExpiresUtc,
            entity.CreatedUtc,
            entity.UpdatedUtc);

        private static AccessRequest ToDomain(AccessRequestEntity entity)
        {
            UserIdentity? reviewer = CreateReviewerIdentity(entity);

            return AccessRequest.Restore(
                entity.Id,
                entity.UserAccountId,
                entity.Organization,
                entity.Purpose,
                entity.Message,
                entity.Status,
                entity.RequestedUtc,
                entity.ReviewedUtc,
                reviewer);
        }

        private static UserIdentity? CreateReviewerIdentity(AccessRequestEntity entity)
        {
            bool hasTenantId = !string.IsNullOrWhiteSpace(entity.ReviewedByTenantId);
            bool hasUserId = !string.IsNullOrWhiteSpace(entity.ReviewedByUserId);

            if (!hasTenantId && !hasUserId)
            {
                return null;
            }

            if (!hasTenantId || !hasUserId)
            {
                throw new InvalidOperationException("Persisted access request reviewer identity is incomplete.");
            }

            return new UserIdentity(entity.ReviewedByTenantId!, entity.ReviewedByUserId!);
        }
    }
}