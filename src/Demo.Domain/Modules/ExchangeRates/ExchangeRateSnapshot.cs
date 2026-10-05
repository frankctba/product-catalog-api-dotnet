namespace Demo.Domain.Modules.ExchangeRates
{
    /// <summary>
    /// One set of rates published by a provider at a point in time. Snapshots are append-only and form the rate history.
    /// </summary>
    public class ExchangeRateSnapshot
    {
        public long Id { get; set; }
        public required string Provider { get; set; }
        public required string BaseCurrency { get; set; }

        /// <summary>When the provider published the rates (UTC).</summary>
        public DateTime RateTimestampUtc { get; set; }

        /// <summary>When the rates were fetched (UTC).</summary>
        public DateTime FetchedAtUtc { get; set; }

        public List<ExchangeRate> Rates { get; set; } = [];
    }
}
