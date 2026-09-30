using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProductCare.Application.ProductIdentification;
using ProductCare.Application.Products;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductCare.IntegrationTests.Persistence
{
    public sealed class ProductCatalogOwnershipTests
    {
        [Fact]
        public async Task Products_AreScopedToOwner()
        {
            using SqliteConnection connection = new("Data Source=:memory:");

            await connection.OpenAsync();

            DbContextOptions<ProductCareDbContext> options = new DbContextOptionsBuilder<ProductCareDbContext>()
                                                                        .UseSqlite(connection)
                                                                        .Options;

            await using ProductCareDbContext context = new(options);

            await context.Database.EnsureCreatedAsync();

            EfProductCatalog catalog = new(context);

            ProductOwner firstUser = new("tenant-1", "user-1");

            ProductOwner secondUser = new("tenant-1", "user-2");

            ProductRecord firstProduct = await catalog.AddAsync( firstUser, new NewProduct(
                                                                    Manufacturer: "Contoso",
                                                                    ModelNumber: "A100",
                                                                    ProductType: "Test Product",
                                                                    ProductName: "First Product",
                                                                    SerialNumber: null,
                                                                    IdentificationConfidence:
                                                                        ProductIdentificationConfidence.High));

            await catalog.AddAsync( secondUser, new NewProduct(
                                        Manufacturer: "Fabrikam",
                                        ModelNumber: "B200",
                                        ProductType: "Test Product",
                                        ProductName: "Second Product",
                                        SerialNumber: null,
                                        IdentificationConfidence:
                                            ProductIdentificationConfidence.High));

            IReadOnlyList<ProductRecord> firstUserProducts = await catalog.GetAllAsync(firstUser);

            Assert.Single(firstUserProducts);

            Assert.Equal( firstProduct.Id, firstUserProducts[0].Id);

            ProductRecord? productViewedByOtherUser = await catalog.GetAsync(secondUser, firstProduct.Id);

            Assert.Null( productViewedByOtherUser);
        }
    }
}
