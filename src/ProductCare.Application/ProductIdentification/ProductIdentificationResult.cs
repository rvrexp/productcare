using System;
using System.Collections.Generic;
using System.Text;

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
