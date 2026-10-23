using ProductCare.Domain.Identity;

namespace ProductCare.Application.Products
{
    public interface IProductService
    {
        Task<ProductRecord> CreateAsync(UserIdentity owner, NewProduct product, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ProductRecord>> GetAllAsync(UserIdentity owner, CancellationToken cancellationToken = default);

        Task<ProductRecord?> GetAsync(UserIdentity owner, Guid productId, CancellationToken cancellationToken = default);
    }
}