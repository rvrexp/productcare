using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductSafety
{
    public interface IProductSafetyService
    {
        Task<ProductSafetyCheckResult> CheckAsync(ProductSafetyQuery query, CancellationToken cancellationToken = default);
    }
}
