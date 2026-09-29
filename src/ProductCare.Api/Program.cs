using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using ProductCare.Api.Infrastructure.Errors;
using ProductCare.Api.Infrastructure.Middleware;
using ProductCare.Application.ProductIdentification;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Options;
using ProductCare.AI;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // Avoid exposing unnecessary server implementation details.
    options.AddServerHeader = false;
});

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

// Configure authentication only when AzureAd settings are provided 
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (!string.IsNullOrWhiteSpace(azureAdSection["ClientId"]))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(azureAdSection);

    builder.Services.AddAuthorization(options =>
    {
        // API endpoints are protected unless explicitly marked anonymous.
        options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                                        .RequireAuthenticatedUser()
                                        .RequireScope("access_as_user")
                                        .Build();
                        });
}
else
{
    // No Azure AD configured skip authentication/authorization.
}

builder.Services.AddProductCareAI(builder.Configuration);

const string DevelopmentCorsPolicy = "ProductCareWeb";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

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

if (app.Environment.IsDevelopment() && allowedOrigins.Length > 0)
{
    // CORS must run before authentication/authorization for browser requests.
    app.UseCors(DevelopmentCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health probes don't require a user identity.
app.MapHealthChecks("/health/live").AllowAnonymous();

app.MapHealthChecks("/health/ready").AllowAnonymous();

app.Run();

// Required by WebApplicationFactory in integration tests.
public partial class Program;