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
        internal DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();
        internal DbSet<AccessRequestEntity> AccessRequests => Set<AccessRequestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductCareDbContext).Assembly);
        }
    }
}