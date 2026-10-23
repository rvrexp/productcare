using Microsoft.EntityFrameworkCore;
using ProductCare.Application.Products;
using ProductCare.Domain.Identity;
using ProductCare.Domain.Products;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Products
{
    public sealed class EfProductRepository : IProductRepository
    {
        private readonly ProductCareDbContext _dbContext;

        public EfProductRepository(ProductCareDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(product);

            ProductEntity entity = ToEntity(product);

            _dbContext.Products.Add(entity);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Product>> GetAllAsync(UserIdentity owner, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(owner);

            List<ProductEntity> entities = await _dbContext.Products
                .AsNoTracking()
                .Where(product => product.OwnerTenantId == owner.TenantId && product.OwnerUserId == owner.UserId)
                .OrderByDescending(product => product.CreatedUtc)
                .ToListAsync(cancellationToken);

            return entities.Select(ToDomain).ToArray();
        }

        public async Task<Product?> GetAsync(UserIdentity owner, Guid productId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(owner);

            ProductEntity? entity = await _dbContext.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(product => product.Id == productId && product.OwnerTenantId == owner.TenantId && product.OwnerUserId == owner.UserId, cancellationToken);

            return entity is null ? null : ToDomain(entity);
        }

        private static ProductEntity ToEntity(Product product) => new()
        {
            Id = product.Id,
            OwnerTenantId = product.Owner.TenantId,
            OwnerUserId = product.Owner.UserId,
            Manufacturer = product.Manufacturer,
            ModelNumber = product.ModelNumber,
            ProductType = product.ProductType,
            ProductName = product.ProductName,
            SerialNumber = product.SerialNumber,
            IdentificationConfidence = product.IdentificationConfidence,
            CreatedUtc = product.CreatedUtc
        };

        private static Product ToDomain(ProductEntity entity) => Product.Restore(
            entity.Id,
            new UserIdentity(entity.OwnerTenantId, entity.OwnerUserId),
            entity.Manufacturer,
            entity.ModelNumber,
            entity.ProductType,
            entity.ProductName,
            entity.SerialNumber,
            entity.IdentificationConfidence,
            entity.CreatedUtc);
    }
}