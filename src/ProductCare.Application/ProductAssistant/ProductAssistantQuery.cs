using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductAssistant
{
    public sealed record ProductAssistantQuery(string Manufacturer, string ModelNumber, string? ProductName, string Message);
}
