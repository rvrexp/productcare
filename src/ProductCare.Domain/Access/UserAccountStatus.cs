namespace ProductCare.Domain.Access
{
    public enum UserAccountStatus
    {
        Registered,
        PendingApproval,
        Active,
        Rejected,
        Disabled,
        Expired
    }
}