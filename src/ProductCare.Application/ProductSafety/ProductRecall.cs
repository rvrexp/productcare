using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductSafety
{
    public sealed record ProductRecall(
        string RecallNumber,
        string Title,
        DateTime? RecallDate,
        string? Description,
        string? Hazard,
        string? Remedy,
        string? ConsumerContact,
        string? OfficialUrl);
}
