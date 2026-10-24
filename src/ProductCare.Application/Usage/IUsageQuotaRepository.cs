using ProductCare.Domain.Identity;

namespace ProductCare.Application.Usage;

public interface IUsageQuotaRepository
{
    Task<int?> TryConsumeAsync(
        UserIdentity identity,
        UsageOperation operation,
        DateOnly usageDateUtc,
        int dailyLimit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<UsageOperation, int>> GetUsageAsync(
        UserIdentity identity,
        DateOnly usageDateUtc,
        CancellationToken cancellationToken = default);
}