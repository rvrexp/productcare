using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProductCare.Application.Usage;
using ProductCare.Domain.Identity;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Usage;

internal sealed class EfUsageQuotaRepository : IUsageQuotaRepository
{
    private const int MaximumAttempts = 5;

    private readonly ProductCareDbContext _dbContext;

    public EfUsageQuotaRepository(ProductCareDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<int?> TryConsumeAsync(
        UserIdentity identity,
        UsageOperation operation,
        DateOnly usageDateUtc,
        int dailyLimit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (dailyLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyLimit), dailyLimit, "Daily limit must be greater than zero.");
        }

        string operationName = operation.ToString();

        for (int attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            IQueryable<UsageCounterEntity> counterQuery = GetCounterQuery(identity, operationName, usageDateUtc);

            int updated = await counterQuery
                .Where(counter => counter.Count < dailyLimit)
                .ExecuteUpdateAsync(setters => setters.SetProperty(counter => counter.Count, counter => counter.Count + 1), cancellationToken);

            if (updated == 1)
            {
                return await counterQuery.Select(counter => counter.Count).SingleAsync(cancellationToken);
            }

            int? currentCount = await counterQuery
                .Select(counter => (int?)counter.Count)
                .SingleOrDefaultAsync(cancellationToken);

            if (currentCount is not null)
            {
                if (currentCount.Value >= dailyLimit)
                {
                    return null;
                }

                continue;
            }

            UsageCounterEntity counter = new()
            {
                Id = Guid.NewGuid(),
                TenantId = identity.TenantId,
                UserId = identity.UserId,
                Operation = operationName,
                UsageDateUtc = usageDateUtc,
                Count = 1
            };

            _dbContext.UsageCounters.Add(counter);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return 1;
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                _dbContext.Entry(counter).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("The usage counter could not be updated because of repeated concurrent changes.");
    }

    public async Task<IReadOnlyDictionary<UsageOperation, int>> GetUsageAsync(
        UserIdentity identity,
        DateOnly usageDateUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        List<UsageCounterEntity> counters = await _dbContext.UsageCounters
            .AsNoTracking()
            .Where(counter =>
                counter.TenantId == identity.TenantId &&
                counter.UserId == identity.UserId &&
                counter.UsageDateUtc == usageDateUtc)
            .ToListAsync(cancellationToken);

        Dictionary<UsageOperation, int> usage = [];

        foreach (UsageCounterEntity counter in counters)
        {
            if (Enum.TryParse(counter.Operation, ignoreCase: false, out UsageOperation operation))
            {
                usage[operation] = counter.Count;
            }
        }

        return usage;
    }

    private IQueryable<UsageCounterEntity> GetCounterQuery(UserIdentity identity, string operationName, DateOnly usageDateUtc) =>
        _dbContext.UsageCounters.Where(counter =>
            counter.TenantId == identity.TenantId &&
            counter.UserId == identity.UserId &&
            counter.Operation == operationName &&
            counter.UsageDateUtc == usageDateUtc);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}