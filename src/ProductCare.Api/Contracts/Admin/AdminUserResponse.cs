namespace ProductCare.Api.Contracts.Admin;

public sealed record AdminUserResponse(
    Guid Id,
    string? DisplayName,
    string? Email,
    string Status,
    string? Plan,
    DateTime? AccessExpiresUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    bool IsCurrentUser);
