using ProductCare.Domain.Access;

namespace ProductCare.Infrastructure.Persistence.Entities
{
    internal sealed class UserAccountEntity
    {
        public Guid Id { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? DisplayName { get; set; }

        public UserAccountStatus Status { get; set; }

        public UserPlan? Plan { get; set; }

        public DateTime? AccessExpiresUtc { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime UpdatedUtc { get; set; }

        public ICollection<AccessRequestEntity> AccessRequests { get; set; } = [];
    }
}