using Demo.Domain.Modules.ExchangeRates;

namespace Demo.Application.Common.Services
{
    /// <summary>
    /// Port to an external exchange-rate source (e.g. Open Exchange Rates).
    /// </summary>
    public interface IExchangeRateProvider
    {
        Task<ExchangeRateSnapshot> GetLatestRates(string baseCurrency, IReadOnlyCollection<string> currencies, CancellationToken cancellationToken);
    }
}
