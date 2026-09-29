using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductIdentification
{
    public interface IProductIdentificationService
    {
        Task<ProductIdentificationResult> IdentifyAsync(ProductImage image, CancellationToken cancellationToken = default);
    }
}
