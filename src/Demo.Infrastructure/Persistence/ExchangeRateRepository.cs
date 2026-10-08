using Demo.Domain.Modules.ExchangeRates;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Persistence;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly DemoDbContext _dbContext;

    public ExchangeRateRepository(DemoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> SaveSnapshotAsync(ExchangeRateSnapshot snapshot, CancellationToken cancellationToken)
    {
        var alreadyStored = await _dbContext.ExchangeRateSnapshots.AnyAsync(s =>
            s.Provider == snapshot.Provider &&
            s.BaseCurrency == snapshot.BaseCurrency &&
            s.RateTimestampUtc == snapshot.RateTimestampUtc,
            cancellationToken);

        if (alreadyStored)
        {
            return false;
        }

        _dbContext.ExchangeRateSnapshots.Add(snapshot);

        var latestRates = await _dbContext.LatestExchangeRates
            .Where(r => r.BaseCurrency == snapshot.BaseCurrency)
            .ToDictionaryAsync(r => r.QuoteCurrency, cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var rate in snapshot.Rates)
        {
            if (!latestRates.TryGetValue(rate.QuoteCurrency, out var latest))
            {
                _dbContext.LatestExchangeRates.Add(new LatestExchangeRate
                {
                    BaseCurrency = snapshot.BaseCurrency,
                    QuoteCurrency = rate.QuoteCurrency,
                    Rate = rate.Rate,
                    RateTimestampUtc = snapshot.RateTimestampUtc,
                    UpdatedAtUtc = now,
                    Snapshot = snapshot
                });
            }
            else if (latest.RateTimestampUtc < snapshot.RateTimestampUtc)
            {
                // Only move forward: an older publication is kept in the history but never replaces a newer rate.
                latest.Rate = rate.Rate;
                latest.RateTimestampUtc = snapshot.RateTimestampUtc;
                latest.UpdatedAtUtc = now;
                latest.Snapshot = snapshot;
            }
        }

        // A single SaveChanges runs in one transaction: history and latest rates are written together or not at all.
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<LatestExchangeRate>> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken)
    {
        return await _dbContext.LatestExchangeRates
            .AsNoTracking()
            .Where(r => r.BaseCurrency == baseCurrency)
            .ToListAsync(cancellationToken);
    }
}
