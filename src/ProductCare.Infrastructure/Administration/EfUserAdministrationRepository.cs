using Microsoft.EntityFrameworkCore;
using ProductCare.Application.Access;
using ProductCare.Application.Administration;
using ProductCare.Domain.Access;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Administration;

public sealed class EfUserAdministrationRepository : IUserAdministrationRepository
{
    private readonly ProductCareDbContext _dbContext;

    public EfUserAdministrationRepository(ProductCareDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<AdminUserRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserAccountEntity>()
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedUtc)
            .ThenBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Select(user => new AdminUserRecord(
                user.Id,
                user.TenantId,
                user.UserId,
                user.DisplayName,
                user.Email,
                user.Status,
                user.Plan,
                user.AccessExpiresUtc,
                user.CreatedUtc,
                user.UpdatedUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminUserRecord?> GetAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserAccountEntity>()
            .AsNoTracking()
            .Where(user => user.Id == accountId)
            .Select(user => new AdminUserRecord(
                user.Id,
                user.TenantId,
                user.UserId,
                user.DisplayName,
                user.Email,
                user.Status,
                user.Plan,
                user.AccessExpiresUtc,
                user.CreatedUtc,
                user.UpdatedUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AdminUserDetailsRecord?> GetDetailsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        AdminUserRecord? user = await GetAsync(accountId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        IReadOnlyList<AccessRequestRecord> accessRequests = await _dbContext.Set<AccessRequestEntity>()
            .AsNoTracking()
            .Where(request => request.UserAccountId == accountId)
            .OrderByDescending(request => request.RequestedUtc)
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

        return new AdminUserDetailsRecord(
            user.Id,
            user.TenantId,
            user.UserId,
            user.DisplayName,
            user.Email,
            user.Status,
            user.Plan,
            user.AccessExpiresUtc,
            user.CreatedUtc,
            user.UpdatedUtc,
            accessRequests);
    }

    public async Task<AdminUserRecord?> SetStatusAsync(Guid accountId, UserAccountStatus status, DateTime updatedUtc, CancellationToken cancellationToken = default)
    {
        UserAccountEntity? user = await _dbContext.Set<UserAccountEntity>().SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.Status = status;
        user.UpdatedUtc = updatedUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminUserRecord(
            user.Id,
            user.TenantId,
            user.UserId,
            user.DisplayName,
            user.Email,
            user.Status,
            user.Plan,
            user.AccessExpiresUtc,
            user.CreatedUtc,
            user.UpdatedUtc);
    }
}
