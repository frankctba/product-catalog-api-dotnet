using Demo.Domain.Modules.ExchangeRates;
using Demo.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Infrastructure.Persistence;

public static class PersistenceSetup
{
    public const string ConnectionStringName = "DemoDb";

    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<DemoDbContext>(options => options.UseSqlite(connectionString));

        // Scoped, like the DbContext they wrap: one per HTTP request or Hangfire job.
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();

        return services;
    }

    /// <summary>
    /// Applies pending migrations and seeds test data when the catalog is empty.
    /// </summary>
    public static async Task InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DemoDbContext>();

        await db.Database.MigrateAsync();

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Sku = "SKU1", Name = "Name", Price = 103.30M },
                new Product { Sku = "SKU2", Name = "Name", Price = 102.20M });

            await db.SaveChangesAsync();
        }
    }
}
