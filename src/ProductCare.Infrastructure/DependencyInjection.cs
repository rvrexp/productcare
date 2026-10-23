using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductCare.Application.Access;
using ProductCare.Application.Products;
using ProductCare.Application.ProductSafety;
using ProductCare.Infrastructure.Access;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Products;
using ProductCare.Infrastructure.ProductSafety;
using System.Net.Http.Headers;

namespace ProductCare.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProductCareInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("ProductCareDatabase")
                                      ?? throw new InvalidOperationException("Connection string 'ProductCareDatabase' is not configured.");

            services.AddDbContext<ProductCareDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.EnableRetryOnFailure();
                });
            });

            services.AddScoped<IProductRepository, EfProductRepository>();
            services.AddScoped<IUserAccessRepository, EfUserAccessRepository>();

            services.AddHttpClient<IProductSafetyService, CpscProductSafetyService>(client =>
            {
                client.BaseAddress = new Uri("https://www.saferproducts.gov/RestWebServices/");
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            return services;
        }
    }
}