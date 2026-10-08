using Demo.Application.Common;
using Demo.Domain.Modules.ExchangeRates;
using Demo.Domain.Modules.Inventory;
using Microsoft.Extensions.Options;

namespace Demo.UnitTests.TestData;

internal static class SampleData
{
    public static IOptions<CurrencyOptions> CurrencyOptions() => Options.Create(new CurrencyOptions
    {
        BaseCurrency = "USD",
        SupportedCurrencies = ["EUR", "CAD", "GBP", "CHF"]
    });

    public static List<Product> Products() =>
    [
        new() { Sku = "SKU1", Name = "Classic Leather Jacket", Price = 103.30M },
        new() { Sku = "SKU2", Name = "Wool Overcoat", Price = 102.20M },
        new() { Sku = "SKU3", Name = "Canvas Sneakers", Price = 59.99M }
    ];

    public static ExchangeRateSnapshot Snapshot(DateTime rateTimestampUtc, params (string Currency, decimal Rate)[] rates) => new()
    {
        Provider = "Fake",
        BaseCurrency = "USD",
        RateTimestampUtc = rateTimestampUtc,
        FetchedAtUtc = rateTimestampUtc.AddMinutes(1),
        Rates = rates.Select(r => new ExchangeRate { QuoteCurrency = r.Currency, Rate = r.Rate }).ToList()
    };
}
