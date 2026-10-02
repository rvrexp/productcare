using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductSafety
{
    public sealed record ProductSafetyCheckResult( bool SourceAvailable, DateTime CheckedUtc, IReadOnlyList<ProductRecall> Recalls);
}
