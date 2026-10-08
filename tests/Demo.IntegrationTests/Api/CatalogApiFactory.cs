using Demo.Domain.Modules.ExchangeRates;
using Demo.Infrastructure.Persistence;
using Demo.IntegrationTests.Databases;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Demo.IntegrationTests.Api;

/// <summary>
/// Hosts the real API on the test database <typeparamref name="TDatabase"/>. The application's own DbContext
/// configuration is replaced, so the factory does not depend on any provider. The "Testing" environment keeps
/// developer user-secrets (a real API key) out of the tests, and the startup sync is off: tests store the rates they need.
/// </summary>
public sealed class CatalogApiFactory<TDatabase> : WebApplicationFactory<Program>
    where TDatabase : ITestDatabase, new()
{
    private readonly TDatabase _database = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenExchangeRates:AppId"] = "",
            ["ExchangeRateSync:SyncOnStartupWhenMissing"] = "false"
        }));

        builder.ConfigureTestServices(services =>
        {
            // Drop the application's DbContext configuration (provider + connection string) and use the test database.
            services.RemoveAll<IDbContextOptionsConfiguration<DemoDbContext>>();
            services.AddDbContext<DemoDbContext>(_database.Configure);
        });
    }

    public async Task StoreRatesAsync(DateTime publishedAtUtc, params (string Currency, decimal Rate)[] rates)
    {
        using var scope = Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();

        var snapshot = ExchangeRateSnapshot.Create("OpenExchangeRates", "USD", publishedAtUtc, publishedAtUtc,
            rates.ToDictionary(r => r.Currency, r => r.Rate));

        await repository.SaveSnapshotAsync(snapshot, CancellationToken.None);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _database.Dispose();
    }
}
