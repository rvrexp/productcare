using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Persistence.Configurations
{
    internal sealed class UserAccountEntityConfiguration : IEntityTypeConfiguration<UserAccountEntity>
    {
        public void Configure(EntityTypeBuilder<UserAccountEntity> builder)
        {
            builder.ToTable("UserAccounts");

            builder.HasKey(account => account.Id);

            builder.Property(account => account.Id).ValueGeneratedNever();

            builder.Property(account => account.TenantId).IsRequired().HasMaxLength(36);

            builder.Property(account => account.UserId).IsRequired().HasMaxLength(128);

            builder.Property(account => account.Email).HasMaxLength(320);

            builder.Property(account => account.DisplayName).HasMaxLength(200);

            builder.Property(account => account.Status).IsRequired().HasConversion<string>().HasMaxLength(32);

            builder.Property(account => account.Plan).HasConversion<string>().HasMaxLength(16);

            builder.Property(account => account.AccessExpiresUtc);

            builder.Property(account => account.CreatedUtc).IsRequired();

            builder.Property(account => account.UpdatedUtc).IsRequired();

            builder.HasIndex(account => new
            {
                account.TenantId,
                account.UserId
            }).IsUnique();

            builder.HasIndex(account => account.Status);
        }
    }
}