using Demo.Domain.Common;

namespace Demo.Domain.Modules.ExchangeRates
{
    /// <summary>
    /// A single rate inside a snapshot: 1 unit of the snapshot's base currency equals <see cref="Rate"/> units of <see cref="QuoteCurrency"/>.
    /// </summary>
    public class ExchangeRate
    {
        // For EF Core.
        private ExchangeRate()
        {
        }

        internal ExchangeRate(string quoteCurrency, decimal rate)
        {
            CurrencyCodes.EnsureValid(quoteCurrency, "Quote currency");

            if (rate <= 0)
            {
                throw new DomainException($"The exchange rate for {quoteCurrency} must be greater than zero, got {rate}.");
            }

            QuoteCurrency = quoteCurrency;
            Rate = rate;
        }

        public long SnapshotId { get; private set; }
        public string QuoteCurrency { get; private set; } = null!;
        public decimal Rate { get; private set; }
    }
}
