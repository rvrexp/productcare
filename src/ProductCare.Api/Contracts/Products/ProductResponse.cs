using ProductCare.Domain.Products;

namespace ProductCare.Api.Contracts.Products
{
    public sealed record ProductResponse(
        Guid Id,
        string? Manufacturer,
        string? ModelNumber,
        string? ProductType,
        string? ProductName,
        string? SerialNumber,
        ProductIdentificationConfidence? IdentificationConfidence,
        DateTime CreatedUtc);
}
