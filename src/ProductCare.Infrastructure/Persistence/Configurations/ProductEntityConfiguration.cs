using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Persistence.Configurations
{
    internal sealed class ProductEntityConfiguration : IEntityTypeConfiguration<ProductEntity>
    {
        public void Configure(EntityTypeBuilder<ProductEntity> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(product => product.Id);
            builder.Property(product => product.Id).ValueGeneratedNever();
            builder.Property(product => product.OwnerTenantId).IsRequired().HasMaxLength(36);
            builder.Property(product => product.OwnerUserId).IsRequired().HasMaxLength(128);
            builder.Property(product => product.Manufacturer).HasMaxLength(200);
            builder.Property(product => product.ModelNumber).HasMaxLength(100);
            builder.Property(product => product.ProductType).HasMaxLength(100);
            builder.Property(product => product.ProductName).HasMaxLength(200);
            builder.Property(product => product.SerialNumber).HasMaxLength(150);
            builder.Property(product => product.IdentificationConfidence).HasConversion<string>().HasMaxLength(16);
            builder.Property(product => product.CreatedUtc).IsRequired();
            builder.Property(product => product.AssistantConversationId).HasMaxLength(200);

            builder.HasIndex(product => new
            {
                product.OwnerTenantId,
                product.OwnerUserId,
                product.CreatedUtc
            });
        }
    }
}