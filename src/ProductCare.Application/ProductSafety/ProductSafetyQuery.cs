using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductSafety
{
    public sealed record ProductSafetyQuery(string Manufacturer, string ModelNumber, string? ProductName);
}
