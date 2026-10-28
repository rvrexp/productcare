using ProductCare.Domain.Identity;
using ProductCare.Domain.Products;

namespace ProductCare.Application.Products
{
    public interface IProductRepository
    {
        Task AddAsync(Product product, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Product>> GetAllAsync(UserIdentity owner, CancellationToken cancellationToken = default);

        Task<Product?> GetAsync(UserIdentity owner, Guid productId, CancellationToken cancellationToken = default);

        Task SetAssistantConversationIdAsync(UserIdentity owner, Guid productId, string? assistantConversationId, CancellationToken cancellationToken = default);
    }
}