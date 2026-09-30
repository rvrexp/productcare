using ProductCare.Application.ProductIdentification;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.Products
{
    public sealed record NewProduct(
        string? Manufacturer,
        string? ModelNumber,
        string? ProductType,
        string? ProductName,
        string? SerialNumber,
        ProductIdentificationConfidence? IdentificationConfidence);
}
