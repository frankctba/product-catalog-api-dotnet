using Demo.Application.Common;
using Demo.Application.Common.Services;
using Demo.Domain.Modules.ExchangeRates;
using Demo.Domain.Modules.Inventory;
using Microsoft.Extensions.Options;

namespace Demo.UnitTests.TestData;

internal static class SampleData
{
    public static readonly DateTime Monday = new(2026, 9, 28, 5, 0, 0, DateTimeKind.Utc);

    public static IOptions<CurrencyOptions> CurrencyOptions() => Options.Create(new CurrencyOptions
    {
        BaseCurrency = "USD",
        SupportedCurrencies = ["EUR", "CAD", "GBP", "CHF"]
    });

    public static List<Product> Products() =>
    [
        new("SKU1", "Classic Leather Jacket", 103.30M),
        new("SKU2", "Wool Overcoat", 102.20M),
        new("SKU3", "Canvas Sneakers", 59.99M)
    ];

    /// <summary>What a provider returns: USD-based rates published at <paramref name="rateTimestampUtc"/>.</summary>
    public static ProviderRates ProviderRates(DateTime rateTimestampUtc, params (string Currency, decimal Rate)[] rates) =>
        new("Fake", "USD", rateTimestampUtc, rates.ToDictionary(r => r.Currency, r => r.Rate));

    /// <summary>The four supported currencies with the sample rates used across the design documents.</summary>
    public static ProviderRates AllProviderRates(DateTime rateTimestampUtc, decimal eur = 0.9201M) =>
        ProviderRates(rateTimestampUtc, ("EUR", eur), ("CAD", 1.3816M), ("GBP", 0.7905M), ("CHF", 0.8641M));

    public static ExchangeRateSnapshot Snapshot(DateTime rateTimestampUtc, params (string Currency, decimal Rate)[] rates) =>
        ExchangeRateSnapshot.Create("Fake", "USD", rateTimestampUtc, rateTimestampUtc.AddMinutes(1),
            rates.ToDictionary(r => r.Currency, r => r.Rate));
}
