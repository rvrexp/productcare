namespace ProductCare.Web.Api
{
    public sealed record CreateProductRequest(
        string? Manufacturer,
        string? ModelNumber,
        string? ProductType,
        string? ProductName,
        string? SerialNumber,
        string? IdentificationConfidence);
}
