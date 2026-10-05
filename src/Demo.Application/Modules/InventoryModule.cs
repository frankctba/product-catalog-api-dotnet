using Demo.Application.Common;
using Demo.Domain.Common;
using Demo.Domain.Modules.ExchangeRates;
using Demo.Domain.Modules.Inventory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Demo.Application.Modules
{
    public interface IInventoryModule
    {
        Task<ProductDto?> GetProduct(string sku);

        /// <summary>
        /// Lists products with prices in the requested currency (the base currency when none is given).
        /// </summary>
        /// <exception cref="UnsupportedCurrencyException">The currency is not supported.</exception>
        /// <exception cref="ExchangeRateUnavailableException">No rate is stored yet for the currency.</exception>
        Task<PagedResult<ProductDto>> GetProducts(string? currency, int page, int pageSize, CancellationToken cancellationToken);
    }

    public class InventoryModule : IInventoryModule
    {
        private readonly ILogger _logger;
        private readonly IProductRepository _productRepository;
        private readonly IExchangeRateRepository _exchangeRateRepository;
        private readonly CurrencyOptions _currencyOptions;

        public InventoryModule(
            ILogger<InventoryModule> logger,
            IProductRepository productRepository,
            IExchangeRateRepository exchangeRateRepository,
            IOptions<CurrencyOptions> currencyOptions
        )
        {
            _logger = logger;
            _productRepository = productRepository;
            _exchangeRateRepository = exchangeRateRepository;
            _currencyOptions = currencyOptions.Value;
        }

        public async Task<ProductDto?> GetProduct(string sku)
        {
            var product = await _productRepository.GetProduct(sku);

            return product is null
                ? null
                : new ProductDto(product.Sku, product.Name, product.Price, _currencyOptions.BaseCurrency);
        }

        public async Task<PagedResult<ProductDto>> GetProducts(string? currency, int page, int pageSize, CancellationToken cancellationToken)
        {
            var baseCurrency = _currencyOptions.BaseCurrency;
            var targetCurrency = ResolveTargetCurrency(currency);
            var rate = targetCurrency == baseCurrency
                ? (decimal?)null
                : await GetLatestRate(baseCurrency, targetCurrency, cancellationToken);

            var products = await _productRepository.GetProducts(page, pageSize, cancellationToken);

            var items = products.Items
                .Select(p => new ProductDto(
                    p.Sku,
                    p.Name,
                    rate is null ? p.Price : CurrencyConverter.Convert(p.Price, rate.Value),
                    targetCurrency))
                .ToList();

            return new PagedResult<ProductDto>(items, products.Page, products.PageSize, products.TotalCount);
        }

        private string ResolveTargetCurrency(string? currency)
        {
            var baseCurrency = _currencyOptions.BaseCurrency;

            if (string.IsNullOrWhiteSpace(currency))
            {
                return baseCurrency;
            }

            var normalized = currency.Trim().ToUpperInvariant();

            if (normalized == baseCurrency || _currencyOptions.SupportedCurrencies.Contains(normalized))
            {
                return normalized;
            }

            throw new UnsupportedCurrencyException(currency, [baseCurrency, .. _currencyOptions.SupportedCurrencies]);
        }

        private async Task<decimal> GetLatestRate(string baseCurrency, string targetCurrency, CancellationToken cancellationToken)
        {
            var latestRates = await _exchangeRateRepository.GetLatestRates(baseCurrency, cancellationToken);
            var latest = latestRates.FirstOrDefault(r => r.QuoteCurrency == targetCurrency);

            if (latest is null)
            {
                _logger.LogWarning("No exchange rate stored for {BaseCurrency} to {TargetCurrency}", baseCurrency, targetCurrency);
                throw new ExchangeRateUnavailableException(baseCurrency, targetCurrency);
            }

            return latest.Rate;
        }
    }
}
