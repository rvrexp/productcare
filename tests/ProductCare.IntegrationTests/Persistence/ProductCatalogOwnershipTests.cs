using Microsoft.Extensions.DependencyInjection;
using ProductCare.Application.ProductIdentification;
using ProductCare.Application.Products;
using ProductCare.IntegrationTests.Infrastructure;

namespace ProductCare.IntegrationTests.Persistence;

[Collection(ProductCareIntegrationTestCollection.Name)]
public sealed class ProductCatalogOwnershipTests
{
    private readonly ProductCareApiFactory _factory;

    public ProductCatalogOwnershipTests(ProductCareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Products_AreScopedToOwner()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IProductCatalog catalog = scope.ServiceProvider.GetRequiredService<IProductCatalog>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string testId = Guid.NewGuid().ToString("N");

        ProductOwner firstUser = new("tenant-1", $"user-1-{testId}");
        ProductOwner secondUser = new("tenant-1", $"user-2-{testId}");

        ProductRecord firstProduct = await catalog.AddAsync(firstUser, new NewProduct(
            Manufacturer: "Contoso",
            ModelNumber: "A100",
            ProductType: "Test Product",
            ProductName: "First Product",
            SerialNumber: null,
            IdentificationConfidence: ProductIdentificationConfidence.High), cancellationToken);

        await catalog.AddAsync(secondUser, new NewProduct(
            Manufacturer: "Fabrikam",
            ModelNumber: "B200",
            ProductType: "Test Product",
            ProductName: "Second Product",
            SerialNumber: null,
            IdentificationConfidence: ProductIdentificationConfidence.High), cancellationToken);

        IReadOnlyList<ProductRecord> firstUserProducts = await catalog.GetAllAsync(firstUser, cancellationToken);

        Assert.Single(firstUserProducts);
        Assert.Equal(firstProduct.Id, firstUserProducts[0].Id);

        ProductRecord? productViewedByOtherUser = await catalog.GetAsync(secondUser, firstProduct.Id, cancellationToken);

        Assert.Null(productViewedByOtherUser);
    }
}