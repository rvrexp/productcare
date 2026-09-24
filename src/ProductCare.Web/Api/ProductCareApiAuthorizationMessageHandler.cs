using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace ProductCare.Web.Api;

public sealed class ProductCareApiAuthorizationMessageHandler
    : AuthorizationMessageHandler
{
    public ProductCareApiAuthorizationMessageHandler(
        IAccessTokenProvider tokenProvider,
        NavigationManager navigation,
        IConfiguration configuration)
        : base(tokenProvider, navigation)
    {
        var apiBaseUrl =
            configuration["ProductCareApi:BaseUrl"]
            ?? throw new InvalidOperationException(
                "ProductCare API base URL is not configured.");

        var apiScope =
            configuration["ProductCareApi:Scope"]
            ?? throw new InvalidOperationException(
                "ProductCare API scope is not configured.");

        // Never attach the ProductCare token to unrelated destinations.
        ConfigureHandler(
            authorizedUrls: [apiBaseUrl],
            scopes: [apiScope]);
    }
}