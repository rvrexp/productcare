using Microsoft.EntityFrameworkCore;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Persistence
{
    public sealed class ProductCareDbContext : DbContext
    {
        public ProductCareDbContext(DbContextOptions<ProductCareDbContext> options) : base(options)
        {
        }

        internal DbSet<ProductEntity> Products => Set<ProductEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductCareDbContext).Assembly);
        }
    }
}
