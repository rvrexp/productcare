using Microsoft.Extensions.DependencyInjection;
using ProductCare.Application.Products;
using ProductCare.Domain.Identity;
using ProductCare.Domain.Products;
using ProductCare.IntegrationTests.Infrastructure;

namespace ProductCare.IntegrationTests.Persistence;

[Collection(ProductCareIntegrationTestCollection.Name)]
public sealed class ProductRepositoryOwnershipTests
{
    private readonly ProductCareApiFactory _factory;

    public ProductRepositoryOwnershipTests(ProductCareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Products_AreScopedToOwner()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IProductRepository repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string testId = Guid.NewGuid().ToString("N");
        DateTime createdUtc = DateTime.UtcNow;

        UserIdentity firstUser = new("tenant-1", $"user-1-{testId}");
        UserIdentity secondUser = new("tenant-1", $"user-2-{testId}");

        Product firstProduct = Product.Create(
            firstUser,
            manufacturer: "Contoso",
            modelNumber: "A100",
            productType: "Test Product",
            productName: "First Product",
            serialNumber: null,
            identificationConfidence: ProductIdentificationConfidence.High,
            createdUtc);

        Product secondProduct = Product.Create(
            secondUser,
            manufacturer: "Fabrikam",
            modelNumber: "B200",
            productType: "Test Product",
            productName: "Second Product",
            serialNumber: null,
            identificationConfidence: ProductIdentificationConfidence.High,
            createdUtc);

        await repository.AddAsync(firstProduct, cancellationToken);
        await repository.AddAsync(secondProduct, cancellationToken);

        IReadOnlyList<Product> firstUserProducts = await repository.GetAllAsync(firstUser, cancellationToken);

        Assert.Single(firstUserProducts);
        Assert.Equal(firstProduct.Id, firstUserProducts[0].Id);

        Product? productViewedByOtherUser = await repository.GetAsync(secondUser, firstProduct.Id, cancellationToken);

        Assert.Null(productViewedByOtherUser);
    }
}