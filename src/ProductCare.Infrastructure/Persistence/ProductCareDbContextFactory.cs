using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductCare.Infrastructure.Persistence
{
    public sealed class ProductCareDbContextFactory : IDesignTimeDbContextFactory<ProductCareDbContext>
    {
        public ProductCareDbContext CreateDbContext(string[] args)
        {
            string? configuredConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ProductCareDatabase");

            string connectionString = !string.IsNullOrWhiteSpace(configuredConnectionString)
                ? configuredConnectionString
                : GetDesignTimeConnectionString();

            DbContextOptionsBuilder<ProductCareDbContext> optionsBuilder = new();

            optionsBuilder.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure();
            });

            return new ProductCareDbContext(optionsBuilder.Options);
        }

        private static string GetDesignTimeConnectionString()
        {
            if (OperatingSystem.IsWindows())
            {
                return @"Server=(localdb)\MSSQLLocalDB;Database=ProductCareDev;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
            }

            return "Server=localhost;Database=ProductCareDesignTime;Integrated Security=True;TrustServerCertificate=True";
        }
    }
}