using Microsoft.Extensions.DependencyInjection;
using ProductCare.Application.Usage;

namespace ProductCare.Infrastructure.Usage;

public static class UsageQuotaServiceCollectionExtensions
{
    public static IServiceCollection AddProductCareUsageQuotaPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUsageQuotaRepository, EfUsageQuotaRepository>();

        return services;
    }
}