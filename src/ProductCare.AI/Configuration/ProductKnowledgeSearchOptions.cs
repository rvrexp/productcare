using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.AI.Configuration
{
    public sealed class ProductKnowledgeSearchOptions
    {
        public const string SectionName = "ProductKnowledgeSearch";

        public string TenantId { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public string SemanticConfigurationName { get; set; } = string.Empty;
    }
}
