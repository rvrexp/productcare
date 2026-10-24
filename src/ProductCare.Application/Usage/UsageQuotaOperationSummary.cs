namespace ProductCare.Application.Usage;

public sealed record UsageQuotaOperationSummary(
    UsageOperation Operation,
    int DailyLimit,
    int Consumed,
    int Remaining);