using Demo.Domain.Common;

namespace Demo.Domain.Modules.ExchangeRates
{
    /// <summary>
    /// The most recent rate for a currency pair. Updated in the same transaction that appends a snapshot to the history.
    /// </summary>
    public class LatestExchangeRate
    {
        // For EF Core.
        private LatestExchangeRate()
        {
        }

        public string BaseCurrency { get; private set; } = null!;
        public string QuoteCurrency { get; private set; } = null!;
        public decimal Rate { get; private set; }
        public DateTime RateTimestampUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }

        public long SnapshotId { get; private set; }
        public ExchangeRateSnapshot? Snapshot { get; private set; }

        public static LatestExchangeRate Create(ExchangeRateSnapshot snapshot, ExchangeRate rate, DateTime nowUtc)
        {
            var latest = new LatestExchangeRate
            {
                BaseCurrency = snapshot.BaseCurrency,
                QuoteCurrency = rate.QuoteCurrency
            };

            latest.Apply(snapshot, rate, nowUtc);

            return latest;
        }

        /// <summary>
        /// Takes the rate from <paramref name="snapshot"/> only when it is newer than the current one:
        /// an older publication stays in the history but never replaces a newer rate.
        /// </summary>
        /// <returns>True when the rate was updated.</returns>
        public bool UpdateFrom(ExchangeRateSnapshot snapshot, ExchangeRate rate, DateTime nowUtc)
        {
            if (snapshot.BaseCurrency != BaseCurrency || rate.QuoteCurrency != QuoteCurrency)
            {
                throw new DomainException(
                    $"Cannot update the {BaseCurrency}/{QuoteCurrency} rate with a {snapshot.BaseCurrency}/{rate.QuoteCurrency} rate.");
            }

            if (snapshot.RateTimestampUtc <= RateTimestampUtc)
            {
                return false;
            }

            Apply(snapshot, rate, nowUtc);

            return true;
        }

        private void Apply(ExchangeRateSnapshot snapshot, ExchangeRate rate, DateTime nowUtc)
        {
            Rate = rate.Rate;
            RateTimestampUtc = snapshot.RateTimestampUtc;
            UpdatedAtUtc = nowUtc;
            Snapshot = snapshot;
        }
    }
}
