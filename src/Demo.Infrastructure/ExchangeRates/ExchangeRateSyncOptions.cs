namespace Demo.Infrastructure.ExchangeRates;

public class ExchangeRateSyncOptions
{
    public const string SectionName = "ExchangeRateSync";

    /// <summary>Cron expression, evaluated in UTC. Default: every Monday at 06:00 UTC.</summary>
    public string Cron { get; set; } = "0 6 * * 1";

    /// <summary>Enqueue a sync at startup when no rates are stored yet, so conversions work before the first Monday.</summary>
    public bool SyncOnStartupWhenMissing { get; set; } = true;
}
