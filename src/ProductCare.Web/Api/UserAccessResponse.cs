namespace ProductCare.Web.Api;

public sealed record UserAccessResponse(
    string Status,
    string? Plan,
    DateTime? AccessExpiresUtc,
    DateTime? RequestedUtc,
    bool IsAdmin);