using Demo.Application.Common;
using Demo.Application.Common.Services;
using Demo.Domain.Common;
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
        /// <exception cref="ExchangeRateSyncException">A failure retrying will not fix.</exception>
        Task SyncLatestRatesAsync(CancellationToken cancellationToken);

        /// <summary>True when a latest rate is stored for every supported currency.</summary>
        Task<bool> HasLatestRatesAsync(CancellationToken cancellationToken);
    }

    public class ExchangeRateSyncModule : IExchangeRateSyncModule
    {
        private readonly ILogger _logger;
        private readonly IExchangeRateProvider _exchangeRateProvider;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly TimeProvider _timeProvider;
        private readonly CurrencyOptions _currencyOptions;

        public ExchangeRateSyncModule(
            ILogger<ExchangeRateSyncModule> logger,
            IExchangeRateProvider exchangeRateProvider,
            IExchangeRateRepository exchangeRateRepository,
            TimeProvider timeProvider,
            IOptions<CurrencyOptions> currencyOptions
        )
        {
            _logger = logger;
            _exchangeRateProvider = exchangeRateProvider;
            _exchangeRateRepository = exchangeRateRepository;
            _timeProvider = timeProvider;
            _currencyOptions = currencyOptions.Value;
        }

        public async Task SyncLatestRatesAsync(CancellationToken cancellationToken)
        {
            var baseCurrency = _currencyOptions.BaseCurrency;
            var currencies = _currencyOptions.SupportedCurrencies;

            var providerRates = await _exchangeRateProvider.GetLatestRatesAsync(baseCurrency, currencies, cancellationToken);

            var snapshot = CreateSnapshot(providerRates, baseCurrency, currencies);

            var saved = await _exchangeRateRepository.SaveSnapshotAsync(snapshot, cancellationToken);

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

        public async Task<bool> HasLatestRatesAsync(CancellationToken cancellationToken)
        {
            var latestRates = await _exchangeRateRepository.GetLatestRatesAsync(_currencyOptions.BaseCurrency, cancellationToken);

            return _currencyOptions.SupportedCurrencies.All(c => latestRates.Any(r => r.QuoteCurrency == c));
        }

        /// <summary>
        /// Checks the provider answered what was asked for, keeps only the configured currencies,
        /// and builds the domain snapshot (which enforces its own invariants, e.g. positive rates).
        /// </summary>
        private ExchangeRateSnapshot CreateSnapshot(ProviderRates providerRates, string baseCurrency, IReadOnlyCollection<string> currencies)
        {
            if (providerRates.BaseCurrency != baseCurrency)
            {
                throw new ExchangeRateSyncException(
                    $"The exchange-rate provider returned base currency '{providerRates.BaseCurrency}', expected '{baseCurrency}'.");
            }

            var missing = currencies.Where(c => !providerRates.Rates.ContainsKey(c)).ToList();
            if (missing.Count > 0)
            {
                throw new ExchangeRateSyncException(
                    $"The exchange-rate provider did not return rates for: {string.Join(", ", missing)}.");
            }

            var configuredRates = currencies.ToDictionary(c => c, c => providerRates.Rates[c]);

            try
            {
                return ExchangeRateSnapshot.Create(
                    providerRates.Provider,
                    baseCurrency,
                    providerRates.RateTimestampUtc,
                    _timeProvider.GetUtcNow().UtcDateTime,
                    configuredRates);
            }
            catch (DomainException ex)
            {
                throw new ExchangeRateSyncException($"The exchange-rate provider returned invalid data: {ex.Message}", ex);
            }
        }
    }
}
