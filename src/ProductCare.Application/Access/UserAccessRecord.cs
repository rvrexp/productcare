using ProductCare.Domain.Access;

namespace ProductCare.Application.Access
{
    public sealed record UserAccessRecord(
        Guid? UserAccountId,
        UserAccountStatus Status,
        UserPlan? Plan,
        DateTime? AccessExpiresUtc,
        DateTime? RequestedUtc);
}