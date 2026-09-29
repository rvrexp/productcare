using ProductCare.Application.ProductIdentification;

namespace ProductCare.Api.Contracts.ProductIdentification;

public sealed record ProductIdentificationResponse(
    string? Manufacturer,
    string? ModelNumber,
    string? ProductType,
    string? ProductName,
    ProductIdentificationConfidence Confidence,
    IReadOnlyList<string> ObservedText);