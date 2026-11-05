namespace ProductCare.Web.Api;

public sealed record AdminUserDetailsResponse(
    Guid Id,
    string? DisplayName,
    string? Email,
    string Status,
    string? Plan,
    DateTime? AccessExpiresUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    bool IsCurrentUser,
    IReadOnlyList<AdminAccessRequestResponse> AccessRequests);
