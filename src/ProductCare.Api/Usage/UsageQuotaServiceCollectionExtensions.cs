using ProductCare.Application.Usage;
using ProductCare.Infrastructure.Usage;

namespace ProductCare.Api.Usage;

public static class UsageQuotaServiceCollectionExtensions
{
    private const string ConfigurationSectionName = "UsageQuotas";

    public static IServiceCollection AddProductCareUsageQuotas(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetRequiredSection(ConfigurationSectionName);

        UsageQuotaPolicy policy = new(
            GetRequiredPositiveLimit(section, "ProductIdentificationDailyLimit"),
          GetRequiredPositiveLimit(section, "ProductKnowledgeDailyLimit"),
            GetRequiredPositiveLimit(section, "ProductAssistantDailyLimit"));

        services.AddSingleton(policy);
        services.AddScoped<IUsageQuotaService, UsageQuotaService>();
        services.AddProductCareUsageQuotaPersistence();

        return services;
    }

    private static int GetRequiredPositiveLimit(IConfigurationSection section, string name)
    {
        int? value = section.GetValue<int?>(name);

        if (value is null || value <= 0)
        {
            throw new InvalidOperationException($"Configuration value '{ConfigurationSectionName}:{name}' must be greater than zero.");
        }

        return value.Value;
    }
}