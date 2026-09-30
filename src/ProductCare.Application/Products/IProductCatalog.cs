using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Application.Products
{
    public interface IProductCatalog
    {
        Task<ProductRecord> AddAsync(ProductOwner owner, NewProduct product, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProductRecord>> GetAllAsync(ProductOwner owner, CancellationToken cancellationToken = default);

        Task<ProductRecord?> GetAsync(ProductOwner owner, Guid productId, CancellationToken cancellationToken = default);
    }
}
