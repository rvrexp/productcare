using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.AI.Configuration
{
    public sealed class FoundryOptions
    {
        public const string SectionName = "Foundry";

        public string TenantId { get; set; } = string.Empty;

        public string ProjectEndpoint { get; set; } = string.Empty;

        public string ModelDeployment { get; set; } = string.Empty;
    }
}
