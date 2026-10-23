using ProductCare.Domain.Access;

namespace ProductCare.Infrastructure.Persistence.Entities
{
    internal sealed class AccessRequestEntity
    {
        public Guid Id { get; set; }

        public Guid UserAccountId { get; set; }

        public UserAccountEntity UserAccount { get; set; } = null!;

        public string? Organization { get; set; }

        public string Purpose { get; set; } = string.Empty;

        public string? Message { get; set; }

        public AccessRequestStatus Status { get; set; }

        public DateTime RequestedUtc { get; set; }

        public DateTime? ReviewedUtc { get; set; }

        public string? ReviewedByTenantId { get; set; }

        public string? ReviewedByUserId { get; set; }
    }
}