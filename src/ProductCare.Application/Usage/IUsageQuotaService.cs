using ProductCare.Domain.Identity;

namespace ProductCare.Application.Usage;

public interface IUsageQuotaService
{
    Task<UsageQuotaConsumption> ConsumeAsync(
        UserIdentity identity,
        UsageOperation operation,
        CancellationToken cancellationToken = default);

    Task<UsageQuotaSummary> GetSummaryAsync(
        UserIdentity identity,
        CancellationToken cancellationToken = default);
}