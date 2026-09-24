using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductCare.IntegrationTests.Api;

public sealed class SystemEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SystemEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Status_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var response =
            await client.GetAsync("/api/v1/system/status");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}