using Demo.Domain.Modules.ExchangeRates;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Persistence;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly DemoDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ExchangeRateRepository(DemoDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
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

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var rate in snapshot.Rates)
        {
            if (latestRates.TryGetValue(rate.QuoteCurrency, out var latest))
            {
                // The domain decides whether the rate moves forward; older publications are kept in the history only.
                latest.UpdateFrom(snapshot, rate, nowUtc);
            }
            else
            {
                _dbContext.LatestExchangeRates.Add(LatestExchangeRate.Create(snapshot, rate, nowUtc));
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
