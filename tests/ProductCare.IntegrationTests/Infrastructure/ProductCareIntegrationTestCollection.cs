namespace ProductCare.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ProductCareIntegrationTestCollection : ICollectionFixture<ProductCareApiFactory>
{
    public const string Name = "ProductCare integration tests";
}