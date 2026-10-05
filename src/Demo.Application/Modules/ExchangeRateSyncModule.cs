using Demo.Application.Common;
using Demo.Application.Common.Services;
using Demo.Domain.Modules.ExchangeRates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Demo.Application.Modules
{
    public interface IExchangeRateSyncModule
    {
        /// <summary>
        /// Fetches the latest rates for the supported currencies and stores them (history + latest).
        /// Safe to run more than once: an already stored snapshot is skipped.
        /// </summary>
        Task SyncLatestRates(CancellationToken cancellationToken);

        /// <summary>True when a latest rate is stored for every supported currency.</summary>
        Task<bool> HasLatestRates(CancellationToken cancellationToken);
    }

    public class ExchangeRateSyncModule : IExchangeRateSyncModule
    {
        private readonly ILogger _logger;
        private readonly IExchangeRateProvider _exchangeRateProvider;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly CurrencyOptions _currencyOptions;

        public ExchangeRateSyncModule(
            ILogger<ExchangeRateSyncModule> logger,
            IExchangeRateProvider exchangeRateProvider,
            IExchangeRateRepository exchangeRateRepository,
            IOptions<CurrencyOptions> currencyOptions
        )
        {
            _logger = logger;
            _exchangeRateProvider = exchangeRateProvider;
            _exchangeRateRepository = exchangeRateRepository;
            _currencyOptions = currencyOptions.Value;
        }

        public async Task SyncLatestRates(CancellationToken cancellationToken)
        {
            var baseCurrency = _currencyOptions.BaseCurrency;
            var currencies = _currencyOptions.SupportedCurrencies;

            var snapshot = await _exchangeRateProvider.GetLatestRates(baseCurrency, currencies, cancellationToken);

            Validate(snapshot, baseCurrency, currencies);

            // Keep only the configured currencies, in case the provider returns more.
            snapshot.Rates = snapshot.Rates.Where(r => currencies.Contains(r.QuoteCurrency)).ToList();

            var saved = await _exchangeRateRepository.SaveSnapshot(snapshot, cancellationToken);

            if (saved)
            {
                _logger.LogInformation(
                    "Stored {RateCount} exchange rates for base {BaseCurrency} published at {RateTimestampUtc:u}",
                    snapshot.Rates.Count, baseCurrency, snapshot.RateTimestampUtc);
            }
            else
            {
                _logger.LogInformation(
                    "Exchange rates for base {BaseCurrency} published at {RateTimestampUtc:u} are already stored, skipping",
                    baseCurrency, snapshot.RateTimestampUtc);
            }
        }

        public async Task<bool> HasLatestRates(CancellationToken cancellationToken)
        {
            var latestRates = await _exchangeRateRepository.GetLatestRates(_currencyOptions.BaseCurrency, cancellationToken);

            return _currencyOptions.SupportedCurrencies.All(c => latestRates.Any(r => r.QuoteCurrency == c));
        }

        private static void Validate(ExchangeRateSnapshot snapshot, string baseCurrency, IReadOnlyCollection<string> currencies)
        {
            if (!string.Equals(snapshot.BaseCurrency, baseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The exchange-rate provider returned base currency '{snapshot.BaseCurrency}', expected '{baseCurrency}'.");
            }

            var missing = currencies.Where(c => snapshot.Rates.All(r => r.QuoteCurrency != c)).ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The exchange-rate provider did not return rates for: {string.Join(", ", missing)}.");
            }

            var invalid = snapshot.Rates.Where(r => r.Rate <= 0).Select(r => r.QuoteCurrency).ToList();
            if (invalid.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The exchange-rate provider returned non-positive rates for: {string.Join(", ", invalid)}.");
            }
        }
    }
}
