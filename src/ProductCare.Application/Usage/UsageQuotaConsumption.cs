namespace ProductCare.Application.Usage;

public sealed record UsageQuotaConsumption(
    UsageOperation Operation,
    int DailyLimit,
    int Consumed,
    int Remaining,
    DateTime ResetUtc);