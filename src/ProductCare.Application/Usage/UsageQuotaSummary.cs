namespace ProductCare.Application.Usage;

public sealed record UsageQuotaSummary(
    DateTime ResetUtc,
    IReadOnlyList<UsageQuotaOperationSummary> Operations);