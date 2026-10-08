using Demo.Domain.Modules.ExchangeRates;

namespace Demo.UnitTests.TestDoubles;

/// <summary>In-memory version of the repository rules: idempotent snapshots, latest rates only move forward.</summary>
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
                Latest.Add(new LatestExchangeRate { BaseCurrency = snapshot.BaseCurrency, QuoteCurrency = rate.QuoteCurrency, Rate = rate.Rate, RateTimestampUtc = snapshot.RateTimestampUtc });
            }
            else if (latest.RateTimestampUtc < snapshot.RateTimestampUtc)
            {
                latest.Rate = rate.Rate;
                latest.RateTimestampUtc = snapshot.RateTimestampUtc;
            }
        }

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<LatestExchangeRate>> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LatestExchangeRate>>(Latest.Where(r => r.BaseCurrency == baseCurrency).ToList());
}
