namespace ProductCare.Web.Api;

public sealed record UsageQuotaSummaryResponse(
    DateTime ResetUtc,
    IReadOnlyList<UsageQuotaOperationResponse> Operations);

public sealed record UsageQuotaOperationResponse(
    string Operation,
    string DisplayName,
    int DailyLimit,
    int Consumed,
    int Remaining);