namespace Demo.Application.Common.Services
{
    /// <summary>
    /// Rates as returned by an exchange-rate provider, before they are validated and turned into a domain snapshot.
    /// </summary>
    public record ProviderRates(
        string Provider,
        string BaseCurrency,
        DateTime RateTimestampUtc,
        IReadOnlyDictionary<string, decimal> Rates);
}
