using ProductCare.Domain.Products;

namespace ProductCare.Application.ProductIdentification
{
    public sealed record ProductIdentificationResult(
        string? Manufacturer,
        string? ModelNumber,
        string? ProductType,
        string? ProductName,
        ProductIdentificationConfidence Confidence,
        IReadOnlyList<string> ObservedText);
}
