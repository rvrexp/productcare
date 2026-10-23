using ProductCare.Domain.Identity;
using ProductCare.Domain.Products;

namespace ProductCare.Application.Products
{
    public sealed class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly TimeProvider _timeProvider;

        public ProductService(IProductRepository productRepository, TimeProvider timeProvider)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<ProductRecord> CreateAsync(UserIdentity owner, NewProduct product, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(product);

            Product domainProduct = Product.Create(
                owner,
                product.Manufacturer,
                product.ModelNumber,
                product.ProductType,
                product.ProductName,
                product.SerialNumber,
                product.IdentificationConfidence,
                _timeProvider.GetUtcNow().UtcDateTime);

            await _productRepository.AddAsync(domainProduct, cancellationToken);

            return ToRecord(domainProduct);
        }

        public async Task<IReadOnlyList<ProductRecord>> GetAllAsync(UserIdentity owner, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(owner);

            IReadOnlyList<Product> products = await _productRepository.GetAllAsync(owner, cancellationToken);

            return products.Select(ToRecord).ToArray();
        }

        public async Task<ProductRecord?> GetAsync(UserIdentity owner, Guid productId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(owner);

            Product? product = await _productRepository.GetAsync(owner, productId, cancellationToken);

            return product is null ? null : ToRecord(product);
        }

        private static ProductRecord ToRecord(Product product) => new(
            product.Id,
            product.Manufacturer,
            product.ModelNumber,
            product.ProductType,
            product.ProductName,
            product.SerialNumber,
            product.IdentificationConfidence,
            product.CreatedUtc);
    }
}