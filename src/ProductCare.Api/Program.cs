using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using ProductCare.AI;
using ProductCare.Api.AccountLifecycle;
using ProductCare.Api.Authentication;
using ProductCare.Api.Authorization;
using ProductCare.Api.Infrastructure.Errors;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Api.Infrastructure.Middleware;
using ProductCare.Api.Observability;
using ProductCare.Api.RateLimiting;
using ProductCare.Api.Usage;
using ProductCare.Application.Access;
using ProductCare.Application.Products;
using ProductCare.Domain.Identity;
using ProductCare.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Azure.Experimental.EnableGenAITracing", true);

bool captureGenAiContent = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Observability:CaptureGenAIContent");

AppContext.SetSwitch("Azure.Experimental.TraceGenAIMessageContent", captureGenAiContent);

builder.WebHost.ConfigureKestrel(options =>
{
    // Avoid exposing unnecessary server implementation details.
    options.AddServerHeader = false;
});

builder.Services.AddProductCareObservability(builder.Configuration);

builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions
                        .Converters
                        .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy<string>(ProductCareRateLimitPolicies.AccessRequestSubmission, httpContext =>
    {
        string partitionKey;

        if (httpContext.User.TryGetUserIdentity(out UserIdentity? identity) && identity is not null)
        {
            partitionKey = $"{identity.TenantId}:{identity.UserId}";
        }
        else
        {
            partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// Configure authentication only when AzureAd settings are provided.
var azureAdSection = builder.Configuration.GetSection("AzureAd");

if (!string.IsNullOrWhiteSpace(azureAdSection["ClientId"]))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(azureAdSection);

    builder.Services.AddAuthorization(options =>
    {
        AuthorizationPolicy userPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireScope(ProductCarePolicies.AccessAsUserScope)
            .Build();

        options.DefaultPolicy = userPolicy;
        options.FallbackPolicy = userPolicy;

        options.AddPolicy(ProductCarePolicies.User, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireScope(ProductCarePolicies.AccessAsUserScope);
        });

        options.AddPolicy(ProductCarePolicies.ActiveUser, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireScope(ProductCarePolicies.AccessAsUserScope);
            policy.AddRequirements(new ActiveUserRequirement());
        });

        options.AddPolicy(ProductCarePolicies.Admin, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new NotDisabledRequirement());
            policy.RequireScope(ProductCarePolicies.AccessAsUserScope);
            policy.RequireRole(ProductCarePolicies.AdministratorRole);
        });
        options.AddPolicy(ProductCarePolicies.NotDisabled, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireScope(ProductCarePolicies.AccessAsUserScope);
            policy.AddRequirements(new NotDisabledRequirement());
        });
    });
}
if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException("AzureAd:ClientId must be configured in Production.");
}

builder.Services.AddProductCareAI(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddProductCareInfrastructure(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, NotDisabledAuthorizationHandler>();
builder.Services.AddProductCareUsageQuotas(builder.Configuration);

const string ProductCareWebCorsPolicy = "ProductCareWeb";

string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(ProductCareWebCorsPolicy, policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

builder.Services.AddProductCareEventGridAuthentication(builder.Configuration);
builder.Services.AddProductCareAccountLifecycle();
var app = builder.Build();

app.UseExceptionHandler();

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi()
       .AllowAnonymous();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

if (allowedOrigins.Length > 0)
{
    // CORS must run before authentication/authorization for browser requests.
    app.UseCors(ProductCareWebCorsPolicy);
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

// Health probes don't require a user identity.
app.MapHealthChecks("/health/live")
   .AllowAnonymous();

app.MapHealthChecks("/health/ready")
   .AllowAnonymous();

app.Run();

// Required by WebApplicationFactory in integration tests.
public partial class Program;