using Demo.Domain.Modules.ExchangeRates;

namespace Demo.UnitTests.TestDoubles;

/// <summary>
/// In-memory repository. The "only move forward" rule comes from <see cref="LatestExchangeRate"/> itself,
/// so this fake does not duplicate domain logic; the real EF Core repository is covered by the integration tests.
/// </summary>
internal class FakeExchangeRateRepository : IExchangeRateRepository
{
    public List<ExchangeRateSnapshot> Snapshots { get; } = [];
    public List<LatestExchangeRate> Latest { get; } = [];

    public Task<bool> SaveSnapshotAsync(ExchangeRateSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (Snapshots.Any(s => s.Provider == snapshot.Provider && s.BaseCurrency == snapshot.BaseCurrency && s.RateTimestampUtc == snapshot.RateTimestampUtc))
        {
            return Task.FromResult(false);
        }

        Snapshots.Add(snapshot);

        foreach (var rate in snapshot.Rates)
        {
            var latest = Latest.SingleOrDefault(r => r.BaseCurrency == snapshot.BaseCurrency && r.QuoteCurrency == rate.QuoteCurrency);
            if (latest is null)
            {
                Latest.Add(LatestExchangeRate.Create(snapshot, rate, snapshot.FetchedAtUtc));
            }
            else
            {
                latest.UpdateFrom(snapshot, rate, snapshot.FetchedAtUtc);
            }
        }

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<LatestExchangeRate>> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LatestExchangeRate>>(Latest.Where(r => r.BaseCurrency == baseCurrency).ToList());
}
