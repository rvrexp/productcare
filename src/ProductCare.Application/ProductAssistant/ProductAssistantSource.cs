using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductAssistant
{
    public sealed record ProductAssistantSource(ProductAssistantSourceType Type, string Title, string? Url);
}
