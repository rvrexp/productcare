using Microsoft.Extensions.DependencyInjection;
using ProductCare.AI.ProductLifecycle;
using ProductCare.Application.Accounts;
using ProductCare.Application.Administration;
using ProductCare.Infrastructure.Accounts;
using ProductCare.Infrastructure.Administration;

namespace ProductCare.Api.AccountLifecycle;

public static class AccountLifecycleServiceCollectionExtensions
{
    public static IServiceCollection AddProductCareAccountLifecycle(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAccountDataRepository, EfAccountDataRepository>();
        services.AddScoped<IProductResourceCleanupService, AzureProductResourceCleanupService>();
        services.AddScoped<IAccountLifecycleService, AccountLifecycleService>();
        services.AddScoped<IUserAdministrationRepository, EfUserAdministrationRepository>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();

        return services;
    }
}
