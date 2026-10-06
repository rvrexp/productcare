using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ProductCare.Api.Observability;

public static class DependencyInjection
{
    public static IServiceCollection AddProductCareObservability(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration["ApplicationInsights:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("ProductCare.Api"))
            .WithTracing(tracing =>
            {
                tracing.AddSource("Azure.AI.Projects.*");
                tracing.AddSource(ProductCareTelemetry.ActivitySourceName);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(ProductCareTelemetry.MeterName);
            })
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
            });

        return services;
    }
}