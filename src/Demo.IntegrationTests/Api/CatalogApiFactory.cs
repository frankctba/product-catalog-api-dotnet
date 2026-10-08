using Demo.Domain.Modules.ExchangeRates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.IntegrationTests.Api;

/// <summary>
/// Hosts the real API on a temporary SQLite file. The "Testing" environment keeps developer user-secrets
/// (a real API key) out of the tests, and the startup sync is off: tests store the rates they need.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"demo-api-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DemoDb"] = $"Data Source={_databasePath}",
            ["OpenExchangeRates:AppId"] = "",
            ["ExchangeRateSync:SyncOnStartupWhenMissing"] = "false"
        }));
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

        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            File.Delete(file);
        }
    }
}
