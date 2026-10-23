using ProductCare.Domain.Access;

namespace ProductCare.Application.Access
{
    public sealed record AccessRequestRecord(
        Guid Id,
        Guid UserAccountId,
        string? Email,
        string? DisplayName,
        string? Organization,
        string Purpose,
        string? Message,
        AccessRequestStatus Status,
        DateTime RequestedUtc,
        DateTime? ReviewedUtc);
}