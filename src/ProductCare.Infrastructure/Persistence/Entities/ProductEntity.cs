using ProductCare.Domain.Products;

namespace ProductCare.Infrastructure.Persistence.Entities
{
    internal sealed class ProductEntity
    {
        public Guid Id { get; set; }

        public string OwnerTenantId { get; set; } = string.Empty;

        public string OwnerUserId { get; set; } = string.Empty;

        public string? Manufacturer { get; set; }

        public string? ModelNumber { get; set; }

        public string? ProductType { get; set; }

        public string? ProductName { get; set; }

        public string? SerialNumber { get; set; }

        public ProductIdentificationConfidence? IdentificationConfidence { get; set; }

        public DateTime CreatedUtc { get; set; }

        public string? AssistantConversationId { get; set; }
    }
}