using Microsoft.EntityFrameworkCore;
using ProductCare.Application.Products;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.Infrastructure.Products
{
    public sealed class EfProductCatalog : IProductCatalog
    {
        private readonly ProductCareDbContext _dbContext;

        public EfProductCatalog(ProductCareDbContext dbContext)

        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<ProductRecord> AddAsync(ProductOwner owner, NewProduct product, CancellationToken cancellationToken = default)
        {
            ValidateOwner(owner);
            ArgumentNullException.ThrowIfNull(product);

            string? manufacturer = Normalize(product.Manufacturer);

            string? modelNumber = Normalize(product.ModelNumber);

            string? productType = Normalize(product.ProductType);

            string? productName = Normalize(product.ProductName);

            string? serialNumber = Normalize(product.SerialNumber);

            if (manufacturer is null && modelNumber is null && productType is null && productName is null)
            {
                throw new ArgumentException("At least one product identifying value is required.", nameof(product));
            }

            ProductEntity entity = new()
            {
                Id = Guid.NewGuid(),

                OwnerTenantId = owner.TenantId,

                OwnerSubject = owner.Subject,

                Manufacturer = manufacturer,

                ModelNumber = modelNumber,

                ProductType = productType,

                ProductName = productName,

                SerialNumber = serialNumber,

                IdentificationConfidence = product.IdentificationConfidence,

                CreatedUtc = DateTime.UtcNow
            };

            _dbContext.Products.Add(entity);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToRecord(entity);
        }

        public async Task<IReadOnlyList<ProductRecord>> GetAllAsync(ProductOwner owner, CancellationToken cancellationToken = default)
        {
            ValidateOwner(owner);

            return await _dbContext.Products
                .AsNoTracking()
                .Where(product => product.OwnerTenantId == owner.TenantId && product.OwnerSubject == owner.Subject)
                .OrderByDescending(product => product.CreatedUtc)
                .Select(product => new ProductRecord(
                            product.Id,
                            product.Manufacturer,
                            product.ModelNumber,
                            product.ProductType,
                            product.ProductName,
                            product.SerialNumber,
                            product.IdentificationConfidence,
                            product.CreatedUtc))
                .ToListAsync(cancellationToken);
        }

        public async Task<ProductRecord?> GetAsync(ProductOwner owner, Guid productId, CancellationToken cancellationToken = default)
        {
            ValidateOwner(owner);

            return await _dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == productId
                        && product.OwnerTenantId == owner.TenantId
                        && product.OwnerSubject == owner.Subject)
                .Select(
                    product =>
                        new ProductRecord(
                            product.Id,
                            product.Manufacturer,
                            product.ModelNumber,
                            product.ProductType,
                            product.ProductName,
                            product.SerialNumber,
                            product.IdentificationConfidence,
                            product.CreatedUtc))
                .SingleOrDefaultAsync(cancellationToken);
        }

        private static ProductRecord ToRecord(ProductEntity entity) => new(
                                                        entity.Id,
                                                        entity.Manufacturer,
                                                        entity.ModelNumber,
                                                        entity.ProductType,
                                                        entity.ProductName,
                                                        entity.SerialNumber,
                                                        entity.IdentificationConfidence,
                                                        entity.CreatedUtc);

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void ValidateOwner(ProductOwner owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            if (string.IsNullOrWhiteSpace(owner.TenantId))
            {
                throw new ArgumentException("Tenant ID is required.", nameof(owner));
            }

            if (string.IsNullOrWhiteSpace(owner.Subject))
            {
                throw new ArgumentException("User subject is required.", nameof(owner));
            }
        }
    }
}
