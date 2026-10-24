namespace ProductCare.Infrastructure.Persistence.Entities;

internal sealed class UsageCounterEntity
{
    public Guid Id { get; set; }

    public string TenantId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public DateOnly UsageDateUtc { get; set; }

    public int Count { get; set; }

    public byte[] RowVersion { get; set; } = [];
}