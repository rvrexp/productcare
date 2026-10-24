using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCare.Infrastructure.Persistence.Entities;

namespace ProductCare.Infrastructure.Persistence.Configurations;

internal sealed class UsageCounterConfiguration : IEntityTypeConfiguration<UsageCounterEntity>
{
    public void Configure(EntityTypeBuilder<UsageCounterEntity> builder)
    {
        builder.ToTable("UsageCounters");

        builder.HasKey(counter => counter.Id);

        builder.Property(counter => counter.TenantId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(counter => counter.UserId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(counter => counter.Operation)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(counter => counter.UsageDateUtc)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(counter => counter.Count)
            .IsRequired();

        builder.Property(counter => counter.RowVersion)
            .IsRowVersion();

        builder.HasIndex(counter => new
        {
            counter.TenantId,
            counter.UserId,
            counter.Operation,
            counter.UsageDateUtc
        })
        .IsUnique();
    }
}