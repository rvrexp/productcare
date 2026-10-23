namespace ProductCare.Web.Api;

public sealed record AdminAccessRequestResponse(
    Guid Id,
    string? Email,
    string? DisplayName,
    string? Organization,
    string Purpose,
    string? Message,
    string Status,
    DateTime RequestedUtc,
    DateTime? ReviewedUtc);