namespace ProductCare.Api.Contracts.Access
{
    public sealed record UserAccessResponse(
        string Status,
        string? Plan,
        DateTime? AccessExpiresUtc,
        DateTime? RequestedUtc,
        bool IsAdmin);
}