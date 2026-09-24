using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using ProductCare.Api.Infrastructure.Errors;
using ProductCare.Api.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // Avoid exposing unnecessary server implementation details.
    options.AddServerHeader = false;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    // API endpoints are protected unless explicitly marked anonymous.
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireScope("access_as_user")
            .Build();
});

const string DevelopmentCorsPolicy =
    "ProductCareWeb";

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? [];

if (builder.Environment.IsDevelopment()
    && allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            DevelopmentCorsPolicy,
            policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });
}

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

if (app.Environment.IsDevelopment()
    && allowedOrigins.Length > 0)
{
    // CORS must run before authentication/authorization for browser requests.
    app.UseCors(DevelopmentCorsPolicy);
}

app.UseAuthentication();
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