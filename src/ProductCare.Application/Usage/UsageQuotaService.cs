using ProductCare.Application.Common.Exceptions;
using ProductCare.Domain.Identity;

namespace ProductCare.Application.Usage;

public sealed class UsageQuotaService : IUsageQuotaService
{
    private static readonly UsageOperation[] TrackedOperations =
    [
        UsageOperation.ProductIdentification,
        UsageOperation.ProductKnowledge,
        UsageOperation.ProductAssistant
    ];

    private readonly IUsageQuotaRepository _repository;
    private readonly UsageQuotaPolicy _policy;
    private readonly TimeProvider _timeProvider;

    public UsageQuotaService(IUsageQuotaRepository repository, UsageQuotaPolicy policy, TimeProvider timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UsageQuotaConsumption> ConsumeAsync(UserIdentity identity, UsageOperation operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        DateOnly usageDateUtc = DateOnly.FromDateTime(utcNow);
        int dailyLimit = _policy.GetDailyLimit(operation);

        int? consumed = await _repository.TryConsumeAsync(identity, operation, usageDateUtc, dailyLimit, cancellationToken);

        DateTime resetUtc = GetResetUtc(usageDateUtc);

        if (consumed is null)
        {
            throw new QuotaExceededException(operation, dailyLimit, resetUtc);
        }

        return new UsageQuotaConsumption(
            operation,
            dailyLimit,
            consumed.Value,
            Math.Max(0, dailyLimit - consumed.Value),
            resetUtc);
    }

    public async Task<UsageQuotaSummary> GetSummaryAsync(UserIdentity identity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        DateOnly usageDateUtc = DateOnly.FromDateTime(utcNow);

        IReadOnlyDictionary<UsageOperation, int> usage = await _repository.GetUsageAsync(identity, usageDateUtc, cancellationToken);

        UsageQuotaOperationSummary[] operations = TrackedOperations
            .Select(operation =>
            {
                int dailyLimit = _policy.GetDailyLimit(operation);
                int consumed = usage.TryGetValue(operation, out int count) ? count : 0;

                return new UsageQuotaOperationSummary(
                    operation,
                    dailyLimit,
                    consumed,
                    Math.Max(0, dailyLimit - consumed));
            })
            .ToArray();

        return new UsageQuotaSummary(GetResetUtc(usageDateUtc), operations);
    }

    private static DateTime GetResetUtc(DateOnly usageDateUtc) =>
        DateTime.SpecifyKind(usageDateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
}