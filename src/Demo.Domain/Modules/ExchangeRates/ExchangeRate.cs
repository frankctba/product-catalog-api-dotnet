namespace Demo.Domain.Modules.ExchangeRates
{
    /// <summary>
    /// A single rate inside a snapshot: 1 unit of the snapshot's base currency equals <see cref="Rate"/> units of <see cref="QuoteCurrency"/>.
    /// </summary>
    public class ExchangeRate
    {
        public long SnapshotId { get; set; }
        public required string QuoteCurrency { get; set; }
        public decimal Rate { get; set; }
    }
}
