using ProductCare.Application.Access;
using ProductCare.Domain.Access;

namespace ProductCare.Application.Administration;

public sealed record AdminUserDetailsRecord(
    Guid Id,
    string TenantId,
    string UserId,
    string? DisplayName,
    string? Email,
    UserAccountStatus Status,
    UserPlan? Plan,
    DateTime? AccessExpiresUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    IReadOnlyList<AccessRequestRecord> AccessRequests);
