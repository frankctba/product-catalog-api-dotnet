namespace Demo.Domain.Modules.ExchangeRates
{
    /// <summary>
    /// The most recent rate for a currency pair. Updated in the same transaction that appends a snapshot to the history.
    /// </summary>
    public class LatestExchangeRate
    {
        public required string BaseCurrency { get; set; }
        public required string QuoteCurrency { get; set; }
        public decimal Rate { get; set; }
        public DateTime RateTimestampUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public long SnapshotId { get; set; }
        public ExchangeRateSnapshot? Snapshot { get; set; }
    }
}
