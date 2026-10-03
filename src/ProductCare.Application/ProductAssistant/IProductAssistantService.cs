using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductAssistant
{
    public interface IProductAssistantService
    {
        Task<ProductAssistantResult> AskAsync(ProductAssistantQuery query, CancellationToken cancellationToken = default);
    }
}
