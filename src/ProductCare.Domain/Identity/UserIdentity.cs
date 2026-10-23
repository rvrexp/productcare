
namespace ProductCare.Domain.Identity
{
    public sealed record UserIdentity
    {
        public UserIdentity(string tenantId, string userId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.", nameof(userId));
            }

            TenantId = tenantId.Trim();
            UserId = userId.Trim();
        }

        public string TenantId { get; }

        public string UserId { get; }
    }
}