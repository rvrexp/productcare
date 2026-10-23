using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Persistence.Configurations
{
    internal sealed class AccessRequestEntityConfiguration : IEntityTypeConfiguration<AccessRequestEntity>
    {
        public void Configure(EntityTypeBuilder<AccessRequestEntity> builder)
        {
            builder.ToTable("AccessRequests");

            builder.HasKey(request => request.Id);

            builder.Property(request => request.Id).ValueGeneratedNever();

            builder.Property(request => request.Organization).HasMaxLength(200);

            builder.Property(request => request.Purpose).IsRequired().HasMaxLength(100);

            builder.Property(request => request.Message).HasMaxLength(2000);

            builder.Property(request => request.Status).IsRequired().HasConversion<string>().HasMaxLength(16);

            builder.Property(request => request.RequestedUtc).IsRequired();

            builder.Property(request => request.ReviewedUtc);

            builder.Property(request => request.ReviewedByTenantId).HasMaxLength(36);

            builder.Property(request => request.ReviewedByUserId).HasMaxLength(128);

            builder.HasOne(request => request.UserAccount)
                .WithMany(account => account.AccessRequests)
                .HasForeignKey(request => request.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(request => new
            {
                request.Status,
                request.RequestedUtc
            });

            builder.HasIndex(request => new
            {
                request.UserAccountId,
                request.RequestedUtc
            });

            builder.HasIndex(request => request.UserAccountId)
                .IsUnique()
                .HasFilter("[Status] = 'Pending'");
        }
    }
}