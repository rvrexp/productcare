using ProductCare.Application.ProductIdentification;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.Products
{
    public sealed record ProductRecord(
        Guid Id,
        string? Manufacturer,
        string? ModelNumber,
        string? ProductType,
        string? ProductName,
        string? SerialNumber,
        ProductIdentificationConfidence? IdentificationConfidence,
        DateTime CreatedUtc);
}
