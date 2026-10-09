using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using ProductCare.IntegrationTests.Infrastructure;

namespace ProductCare.IntegrationTests.Api;

[Collection(ProductCareIntegrationTestCollection.Name)]
public sealed class HealthEndpointTests
{
    private readonly ProductCareApiFactory _factory;

    public HealthEndpointTests(ProductCareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}