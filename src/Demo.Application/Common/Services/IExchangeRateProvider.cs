namespace Demo.Application.Common.Services
{
    /// <summary>
    /// Port to an external exchange-rate source (e.g. Open Exchange Rates).
    /// </summary>
    public interface IExchangeRateProvider
    {
        /// <exception cref="ExchangeRateSyncException">A failure retrying will not fix, such as a missing or rejected API key.</exception>
        /// <exception cref="HttpRequestException">A transient failure, such as a network error or a 5xx response.</exception>
        Task<ProviderRates> GetLatestRatesAsync(string baseCurrency, IReadOnlyCollection<string> currencies, CancellationToken cancellationToken);
    }
}
