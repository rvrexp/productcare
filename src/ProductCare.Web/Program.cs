using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProductCare.Web;
using ProductCare.Web.Api;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl =
    builder.Configuration["ProductCareApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "ProductCare API base URL is not configured.");

var apiScope =
    builder.Configuration["ProductCareApi:Scope"]
    ?? throw new InvalidOperationException(
        "ProductCare API scope is not configured.");

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

    // Request the ProductCare API scope as part of the user session.
    options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);

    // Redirect mode avoids relying on browser popup behavior.
    options.ProviderOptions.LoginMode = "redirect";
});

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<ProductCareApiAuthorizationMessageHandler>();

builder.Services.AddHttpClient<ProductCareApiClient>(client =>
                    {
                        client.BaseAddress = new Uri(apiBaseUrl);
                    }).AddHttpMessageHandler<ProductCareApiAuthorizationMessageHandler>();

await builder.Build().RunAsync();