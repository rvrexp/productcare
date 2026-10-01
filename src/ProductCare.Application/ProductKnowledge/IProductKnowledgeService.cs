using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.ProductKnowledge
{
    public interface IProductKnowledgeService
    {
        Task<ProductKnowledgeAnswer> AskAsync(ProductKnowledgeQuery query, CancellationToken cancellationToken = default);
    }
}
