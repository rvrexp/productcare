using ProductCare.Api.Infrastructure.Errors;
using ProductCare.Api.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Do not advertise the web server implementation in response headers.
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// API
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Standard RFC-compatible error responses.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Health checks will later include database/search/AI dependencies.
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();

// Allows WebApplicationFactory to host the API during integration tests.
public partial class Program;