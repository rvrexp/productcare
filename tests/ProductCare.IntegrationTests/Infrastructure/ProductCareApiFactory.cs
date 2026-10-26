using System.Data.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductCare.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace ProductCare.IntegrationTests.Infrastructure;

public sealed class ProductCareApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "ProductCareIntegrationTests";
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
    private string _connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _sqlServer.StartAsync(TestContext.Current.CancellationToken);
        await _sqlServer.ExecScriptAsync($"CREATE DATABASE [{DatabaseName}];", TestContext.Current.CancellationToken);

        DbConnectionStringBuilder connectionStringBuilder = new() { ConnectionString = _sqlServer.GetConnectionString() };
        connectionStringBuilder["Database"] = DatabaseName;
        _connectionString = connectionStringBuilder.ConnectionString;

        using IServiceScope scope = Services.CreateScope();
        ProductCareDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProductCareDbContext>();
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            Dictionary<string, string?> settings = new()
            {
                [HostDefaults.EnvironmentKey] = "Testing",
                ["ConnectionStrings:ProductCareDatabase"] = _connectionString,
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
                ["AzureAd:Scopes"] = "access_as_user",
                ["Foundry:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["Foundry:ProjectEndpoint"] = "https://productcare-test.services.ai.azure.com/api/projects/productcare-test",
                ["Foundry:ModelDeployment"] = "productcare-test",
                ["ProductKnowledgeSearch:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["ProductKnowledgeSearch:Endpoint"] = "https://productcare-test.search.windows.net",
                ["ProductKnowledgeSearch:IndexName"] = "productcare-manuals",
                ["ProductKnowledgeSearch:SemanticConfigurationName"] = "productcare-semantic",
                ["ProductManualIngestion:StorageServiceUri"] = "https://productcaretest.blob.core.windows.net",
                ["ProductManualIngestion:ContainerName"] = "product-manuals-test",
                ["ProductManualIngestion:IndexerName"] = "productcare-manuals-test-indexer",
            };

            configuration.AddInMemoryCollection(settings);
        });

        return base.CreateHost(builder);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }
}