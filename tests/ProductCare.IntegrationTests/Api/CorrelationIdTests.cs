using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductCare.IntegrationTests.Api;

public sealed class CorrelationIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorrelationIdTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Response_ContainsCorrelationId()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/v1/system/status");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }
}