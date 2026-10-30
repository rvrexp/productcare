using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ProductCare.Api.Authentication;

public static class EventGridAuthenticationDefaults
{
    public const string Scheme = "EventGridBearer";
    public const string Policy = "ProductCare.EventGridWebhook";
}

public static class EventGridAuthenticationExtensions
{
    public static IServiceCollection AddProductCareEventGridAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? tenantId = configuration["EventGridAuthentication:TenantId"];
        string? audience = configuration["EventGridAuthentication:Audience"];
        string? allowedCallerObjectId = configuration["EventGridAuthentication:AllowedCallerObjectId"];

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(allowedCallerObjectId))
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(EventGridAuthenticationDefaults.Policy, policy => policy.RequireAssertion(_ => false));
            });

            return services;
        }

        string v2Issuer = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        string v1Issuer = $"https://sts.windows.net/{tenantId}/";

        services.AddAuthentication()
            .AddJwtBearer(EventGridAuthenticationDefaults.Scheme, options =>
            {
                options.Authority = v2Issuer;
                options.Audience = audience;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = [v2Issuer, v1Issuer],
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(EventGridAuthenticationDefaults.Policy, policy =>
            {
                policy.AuthenticationSchemes.Add(EventGridAuthenticationDefaults.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("oid", allowedCallerObjectId);
            });
        });

        return services;
    }
}