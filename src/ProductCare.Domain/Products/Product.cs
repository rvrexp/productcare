using ProductCare.Domain.Identity;

namespace ProductCare.Domain.Products
{
    public sealed class Product
    {
        private Product(Guid id, UserIdentity owner, string? manufacturer, string? modelNumber, string? productType, string? productName, string? serialNumber, ProductIdentificationConfidence? identificationConfidence, DateTime createdUtc)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Product ID is required.", nameof(id));
            }

            ArgumentNullException.ThrowIfNull(owner);

            string? normalizedManufacturer = Normalize(manufacturer);
            string? normalizedModelNumber = Normalize(modelNumber);
            string? normalizedProductType = Normalize(productType);
            string? normalizedProductName = Normalize(productName);
            string? normalizedSerialNumber = Normalize(serialNumber);

            if (normalizedManufacturer is null && normalizedModelNumber is null && normalizedProductType is null && normalizedProductName is null)
            {
                throw new ArgumentException("At least one product identifying value is required.");
            }

            Id = id;
            Owner = owner;
            Manufacturer = normalizedManufacturer;
            ModelNumber = normalizedModelNumber;
            ProductType = normalizedProductType;
            ProductName = normalizedProductName;
            SerialNumber = normalizedSerialNumber;
            IdentificationConfidence = identificationConfidence;
            CreatedUtc = createdUtc;
        }

        public Guid Id { get; }

        public UserIdentity Owner { get; }

        public string? Manufacturer { get; }

        public string? ModelNumber { get; }

        public string? ProductType { get; }

        public string? ProductName { get; }

        public string? SerialNumber { get; }

        public ProductIdentificationConfidence? IdentificationConfidence { get; }

        public DateTime CreatedUtc { get; }

        public static Product Create(UserIdentity owner, string? manufacturer, string? modelNumber, string? productType, string? productName, string? serialNumber, ProductIdentificationConfidence? identificationConfidence, DateTime createdUtc)
        {
            if (createdUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Created time must be UTC.", nameof(createdUtc));
            }

            return new Product(Guid.NewGuid(), owner, manufacturer, modelNumber, productType, productName, serialNumber, identificationConfidence, createdUtc);
        }

        public static Product Restore(Guid id, UserIdentity owner, string? manufacturer, string? modelNumber, string? productType, string? productName, string? serialNumber, ProductIdentificationConfidence? identificationConfidence, DateTime createdUtc)
        {
            if (createdUtc.Kind == DateTimeKind.Local)
            {
                throw new ArgumentException("Persisted created time cannot be local time.", nameof(createdUtc));
            }

            DateTime normalizedCreatedUtc = DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc);

            return new Product(id, owner, manufacturer, modelNumber, productType, productName, serialNumber, identificationConfidence, normalizedCreatedUtc);
        }

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}