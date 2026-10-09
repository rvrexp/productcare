using Microsoft.AspNetCore.Mvc.Testing;
using ProductCare.IntegrationTests.Infrastructure;

namespace ProductCare.IntegrationTests.Api;

[Collection(ProductCareIntegrationTestCollection.Name)]
public sealed class CorrelationIdTests
{
    private readonly ProductCareApiFactory _factory;

    public CorrelationIdTests(ProductCareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Response_ContainsCorrelationId()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/api/v1/system/status", TestContext.Current.CancellationToken);

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }
}