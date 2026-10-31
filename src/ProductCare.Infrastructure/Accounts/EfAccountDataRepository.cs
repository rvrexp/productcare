using Microsoft.EntityFrameworkCore;
using ProductCare.Application.Accounts;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Accounts;

public sealed class EfAccountDataRepository : IAccountDataRepository
{
    private readonly ProductCareDbContext _dbContext;

    public EfAccountDataRepository(ProductCareDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<AccountProductRecord?> GetProductAsync(string tenantId, string userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ProductEntity>()
            .AsNoTracking()
            .Where(product => product.Id == productId && product.OwnerTenantId == tenantId && product.OwnerUserId == userId)
            .Select(product => new AccountProductRecord(product.Id, product.AssistantConversationId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountProductRecord>> GetProductsAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ProductEntity>()
            .AsNoTracking()
            .Where(product => product.OwnerTenantId == tenantId && product.OwnerUserId == userId)
            .OrderBy(product => product.CreatedUtc)
            .Select(product => new AccountProductRecord(product.Id, product.AssistantConversationId))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteProductAsync(string tenantId, string userId, Guid productId, CancellationToken cancellationToken = default)
    {
        int deleted = await _dbContext.Set<ProductEntity>()
            .Where(product => product.Id == productId && product.OwnerTenantId == tenantId && product.OwnerUserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    public async Task PurgeOwnedDataAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        UserAccountEntity? account = await GetAccountAsync(tenantId, userId, cancellationToken);

        if (account is not null)
        {
            await _dbContext.Set<AccessRequestEntity>().Where(request => request.UserAccountId == account.Id).ExecuteDeleteAsync(cancellationToken);
        }

        await _dbContext.Set<UsageCounterEntity>().Where(counter => counter.TenantId == tenantId && counter.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Set<ProductEntity>().Where(product => product.OwnerTenantId == tenantId && product.OwnerUserId == userId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteAccountDataAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        UserAccountEntity? account = await GetAccountAsync(tenantId, userId, cancellationToken);

        await PurgeOwnedDataAsync(tenantId, userId, cancellationToken);

        if (account is not null)
        {
            await _dbContext.Set<UserAccountEntity>().Where(user => user.Id == account.Id).ExecuteDeleteAsync(cancellationToken);
        }
    }

    private Task<UserAccountEntity?> GetAccountAsync(string tenantId, string userId, CancellationToken cancellationToken) =>
        _dbContext.Set<UserAccountEntity>().AsNoTracking().SingleOrDefaultAsync(user => user.TenantId == tenantId && user.UserId == userId, cancellationToken);
}
