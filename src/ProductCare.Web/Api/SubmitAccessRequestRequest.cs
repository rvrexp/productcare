namespace ProductCare.Web.Api;

public sealed record SubmitAccessRequestRequest(
    string? Organization,
    string Purpose,
    string? Message);