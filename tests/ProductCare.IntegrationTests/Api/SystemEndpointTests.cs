using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using ProductCare.IntegrationTests.Infrastructure;

namespace ProductCare.IntegrationTests.Api;

[Collection(ProductCareIntegrationTestCollection.Name)]
public sealed class SystemEndpointTests
{
    private readonly ProductCareApiFactory _factory;

    public SystemEndpointTests(ProductCareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Status_WithoutAuthentication_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/api/v1/system/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}