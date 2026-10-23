namespace ProductCare.Application.Access
{
    public sealed record NewAccessRequest(
        string? Organization,
        string Purpose,
        string? Message);
}