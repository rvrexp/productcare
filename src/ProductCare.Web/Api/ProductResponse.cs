namespace ProductCare.Web.Api
{
    public sealed record ProductResponse(
        Guid Id,
        string? Manufacturer,
        string? ModelNumber,
        string? ProductType,
        string? ProductName,
        string? SerialNumber,
        string? IdentificationConfidence,
        DateTime CreatedUtc);
}
