using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductAssistant
{
    public sealed record ProductAssistantResult(string Answer, IReadOnlyList<ProductAssistantSource> Sources, IReadOnlyList<string> ToolsUsed);
}
