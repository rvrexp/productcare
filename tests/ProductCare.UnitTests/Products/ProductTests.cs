using ProductCare.Domain.Identity;
using ProductCare.Domain.Products;

namespace ProductCare.UnitTests.Products;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidProduct_NormalizesValues()
    {
        UserIdentity owner = new("tenant-1", "user-1");
        DateTime createdUtc = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        Product product = Product.Create(
            owner,
            manufacturer: " Contoso ",
            modelNumber: " A100 ",
            productType: " Appliance ",
            productName: " Test Product ",
            serialNumber: " SN123 ",
            identificationConfidence: ProductIdentificationConfidence.High,
            createdUtc);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(owner, product.Owner);
        Assert.Equal("Contoso", product.Manufacturer);
        Assert.Equal("A100", product.ModelNumber);
        Assert.Equal("Appliance", product.ProductType);
        Assert.Equal("Test Product", product.ProductName);
        Assert.Equal("SN123", product.SerialNumber);
        Assert.Equal(ProductIdentificationConfidence.High, product.IdentificationConfidence);
        Assert.Equal(createdUtc, product.CreatedUtc);
    }

    [Fact]
    public void Create_WithoutIdentifyingValue_ThrowsArgumentException()
    {
        UserIdentity owner = new("tenant-1", "user-1");
        DateTime createdUtc = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => Product.Create(
            owner,
            manufacturer: null,
            modelNumber: null,
            productType: null,
            productName: null,
            serialNumber: "SN123",
            identificationConfidence: null,
            createdUtc));
    }

    [Fact]
    public void Create_WithNonUtcCreatedTime_ThrowsArgumentException()
    {
        UserIdentity owner = new("tenant-1", "user-1");
        DateTime localTime = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => Product.Create(
            owner,
            manufacturer: "Contoso",
            modelNumber: null,
            productType: null,
            productName: null,
            serialNumber: null,
            identificationConfidence: null,
            localTime));
    }
}