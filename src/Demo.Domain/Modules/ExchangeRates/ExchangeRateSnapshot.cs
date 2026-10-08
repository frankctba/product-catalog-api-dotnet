using Demo.Domain.Common;

namespace Demo.Domain.Modules.ExchangeRates
{
    /// <summary>
    /// One set of rates published by a provider at a point in time. Snapshots are append-only and form the rate history.
    /// </summary>
    public class ExchangeRateSnapshot
    {
        private readonly List<ExchangeRate> _rates = [];

        // For EF Core.
        private ExchangeRateSnapshot()
        {
        }

        public long Id { get; private set; }
        public string Provider { get; private set; } = null!;
        public string BaseCurrency { get; private set; } = null!;

        /// <summary>When the provider published the rates (UTC).</summary>
        public DateTime RateTimestampUtc { get; private set; }

        /// <summary>When the rates were fetched (UTC).</summary>
        public DateTime FetchedAtUtc { get; private set; }

        public IReadOnlyList<ExchangeRate> Rates => _rates;

        public static ExchangeRateSnapshot Create(
            string provider,
            string baseCurrency,
            DateTime rateTimestampUtc,
            DateTime fetchedAtUtc,
            IReadOnlyDictionary<string, decimal> rates)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new DomainException("An exchange-rate snapshot needs a provider.");
            }

            CurrencyCodes.EnsureValid(baseCurrency, "Base currency");

            if (rateTimestampUtc.Kind != DateTimeKind.Utc || fetchedAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new DomainException("Exchange-rate snapshot timestamps must be in UTC.");
            }

            if (rates.Count == 0)
            {
                throw new DomainException("An exchange-rate snapshot needs at least one rate.");
            }

            if (rates.ContainsKey(baseCurrency))
            {
                throw new DomainException($"An exchange-rate snapshot cannot contain a rate for its own base currency {baseCurrency}.");
            }

            var snapshot = new ExchangeRateSnapshot
            {
                Provider = provider,
                BaseCurrency = baseCurrency,
                RateTimestampUtc = rateTimestampUtc,
                FetchedAtUtc = fetchedAtUtc
            };

            snapshot._rates.AddRange(rates.Select(r => new ExchangeRate(r.Key, r.Value)));

            return snapshot;
        }
    }
}
