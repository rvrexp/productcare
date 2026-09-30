using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductCare.Application.Products;
using ProductCare.Infrastructure.Persistence;
using ProductCare.Infrastructure.Products;
using System;
using System.Collections.Generic;
using System.Text;

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
                                                                options.UseSqlServer(connectionString,
                                                                    sqlServerOptions =>
                                                                      {
                                                                          sqlServerOptions.EnableRetryOnFailure();
                                                                      });
                                                            });

            services.AddScoped<IProductCatalog, EfProductCatalog>();

            return services;
        }
    }
}
