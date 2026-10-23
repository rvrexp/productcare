namespace ProductCare.Api.Contracts.Admin;

public sealed record AccessRequestResponse(
    Guid Id,
    string? Email,
    string? DisplayName,
    string? Organization,
    string Purpose,
    string? Message,
    string Status,
    DateTime RequestedUtc,
    DateTime? ReviewedUtc);