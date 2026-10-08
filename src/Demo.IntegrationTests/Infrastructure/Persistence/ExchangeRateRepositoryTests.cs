using Demo.Domain.Modules.ExchangeRates;
using Demo.Infrastructure.Persistence;
using Demo.IntegrationTests.Databases;
using Microsoft.EntityFrameworkCore;

namespace Demo.IntegrationTests.Infrastructure.Persistence;

/// <summary>
/// Written once, run against every provider: see the derived classes under Databases/.
/// </summary>
public abstract class ExchangeRateRepositoryTests<TDatabase> : IDisposable
    where TDatabase : ITestDatabase, new()
{
    private static readonly DateTime Monday = new(2026, 9, 28, 5, 0, 0, DateTimeKind.Utc);

    private readonly TDatabase _database = new();

    protected ExchangeRateRepositoryTests() => _database.Migrate();

    public void Dispose() => _database.Dispose();

    private static ExchangeRateSnapshot Snapshot(DateTime publishedAt, decimal eur, decimal gbp = 0.7905M, string baseCurrency = "USD") =>
        ExchangeRateSnapshot.Create("OpenExchangeRates", baseCurrency, publishedAt, publishedAt.AddMinutes(1),
            new Dictionary<string, decimal> { ["EUR"] = eur, ["GBP"] = gbp });

    private async Task<bool> SaveAsync(ExchangeRateSnapshot snapshot)
    {
        await using var context = _database.CreateContext();
        return await new ExchangeRateRepository(context, TimeProvider.System).SaveSnapshotAsync(snapshot, CancellationToken.None);
    }

    private async Task<IReadOnlyList<LatestExchangeRate>> GetLatestAsync(string baseCurrency = "USD")
    {
        await using var context = _database.CreateContext();
        return await new ExchangeRateRepository(context, TimeProvider.System).GetLatestRatesAsync(baseCurrency, CancellationToken.None);
    }

    [Fact]
    public async Task SaveSnapshotAsync_StoresSnapshotHistoryAndLatestRates()
    {
        Assert.True(await SaveAsync(Snapshot(Monday, eur: 0.9201M)));

        await using var context = _database.CreateContext();
        var snapshot = await context.ExchangeRateSnapshots.Include(s => s.Rates).SingleAsync();
        Assert.Equal(Monday, snapshot.RateTimestampUtc);
        Assert.Equal(2, snapshot.Rates.Count);

        var latest = await GetLatestAsync();
        Assert.Equal(2, latest.Count);
        Assert.All(latest, r => Assert.Equal(snapshot.Id, r.SnapshotId));
        Assert.Equal(0.9201M, latest.Single(r => r.QuoteCurrency == "EUR").Rate);
    }

    [Fact]
    public async Task SaveSnapshotAsync_SamePublicationTwice_IsStoredOnce()
    {
        Assert.True(await SaveAsync(Snapshot(Monday, eur: 0.9201M)));
        Assert.False(await SaveAsync(Snapshot(Monday, eur: 0.9201M)));

        await using var context = _database.CreateContext();
        Assert.Equal(1, await context.ExchangeRateSnapshots.CountAsync());
        Assert.Equal(2, await context.ExchangeRates.CountAsync());
    }

    [Fact]
    public async Task SaveSnapshotAsync_NewerPublication_KeepsHistoryAndUpdatesLatest()
    {
        await SaveAsync(Snapshot(Monday.AddDays(-7), eur: 0.9184M));
        await SaveAsync(Snapshot(Monday, eur: 0.9201M));

        await using var context = _database.CreateContext();
        Assert.Equal(2, await context.ExchangeRateSnapshots.CountAsync());
        Assert.Equal(4, await context.ExchangeRates.CountAsync());

        var eur = (await GetLatestAsync()).Single(r => r.QuoteCurrency == "EUR");
        Assert.Equal((0.9201M, Monday), (eur.Rate, eur.RateTimestampUtc));
    }

    [Fact]
    public async Task SaveSnapshotAsync_OlderPublicationArrivingLate_IsKeptInHistoryButDoesNotReplaceLatest()
    {
        await SaveAsync(Snapshot(Monday, eur: 0.9201M));
        Assert.True(await SaveAsync(Snapshot(Monday.AddDays(-7), eur: 0.9184M)));

        await using var context = _database.CreateContext();
        Assert.Equal(2, await context.ExchangeRateSnapshots.CountAsync());

        var eur = (await GetLatestAsync()).Single(r => r.QuoteCurrency == "EUR");
        Assert.Equal((0.9201M, Monday), (eur.Rate, eur.RateTimestampUtc));
    }

    [Fact]
    public async Task SaveSnapshotAsync_KeepsEightDecimalPlaces()
    {
        await SaveAsync(Snapshot(Monday, eur: 0.92012345M, gbp: 1.23456789M));

        var latest = await GetLatestAsync();
        Assert.Equal(0.92012345M, latest.Single(r => r.QuoteCurrency == "EUR").Rate);
        Assert.Equal(1.23456789M, latest.Single(r => r.QuoteCurrency == "GBP").Rate);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ReturnsOnlyTheRequestedBase()
    {
        await SaveAsync(Snapshot(Monday, eur: 0.9201M));
        await SaveAsync(ExchangeRateSnapshot.Create("OpenExchangeRates", "CHF", Monday, Monday,
            new Dictionary<string, decimal> { ["EUR"] = 1.06M }));

        Assert.Equal(2, (await GetLatestAsync("USD")).Count);
        Assert.Equal("EUR", Assert.Single(await GetLatestAsync("CHF")).QuoteCurrency);
    }
}
