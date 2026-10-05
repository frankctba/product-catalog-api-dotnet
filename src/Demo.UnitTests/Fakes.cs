using Demo.Application.Common;
using Demo.Application.Common.Services;
using Demo.Domain.Common;
using Demo.Domain.Modules.ExchangeRates;
using Demo.Domain.Modules.Inventory;
using Microsoft.Extensions.Options;

namespace Demo.UnitTests;

internal static class TestData
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

internal class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public FakeProductRepository(List<Product> products) => _products = products;

    public Task<Product?> GetProduct(string sku) => Task.FromResult(_products.SingleOrDefault(p => p.Sku == sku));

    public Task<PagedResult<Product>> GetProducts(int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = _products.OrderBy(p => p.Sku).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<Product>(items, page, pageSize, _products.Count));
    }
}

/// <summary>In-memory version of the repository rules: idempotent snapshots, latest rates only move forward.</summary>
internal class FakeExchangeRateRepository : IExchangeRateRepository
{
    public List<ExchangeRateSnapshot> Snapshots { get; } = [];
    public List<LatestExchangeRate> Latest { get; } = [];

    public Task<bool> SaveSnapshot(ExchangeRateSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (Snapshots.Any(s => s.Provider == snapshot.Provider && s.BaseCurrency == snapshot.BaseCurrency && s.RateTimestampUtc == snapshot.RateTimestampUtc))
        {
            return Task.FromResult(false);
        }

        Snapshots.Add(snapshot);

        foreach (var rate in snapshot.Rates)
        {
            var latest = Latest.SingleOrDefault(r => r.BaseCurrency == snapshot.BaseCurrency && r.QuoteCurrency == rate.QuoteCurrency);
            if (latest is null)
            {
                Latest.Add(new LatestExchangeRate { BaseCurrency = snapshot.BaseCurrency, QuoteCurrency = rate.QuoteCurrency, Rate = rate.Rate, RateTimestampUtc = snapshot.RateTimestampUtc });
            }
            else if (latest.RateTimestampUtc < snapshot.RateTimestampUtc)
            {
                latest.Rate = rate.Rate;
                latest.RateTimestampUtc = snapshot.RateTimestampUtc;
            }
        }

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<LatestExchangeRate>> GetLatestRates(string baseCurrency, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LatestExchangeRate>>(Latest.Where(r => r.BaseCurrency == baseCurrency).ToList());
}

internal class FakeExchangeRateProvider : IExchangeRateProvider
{
    private readonly Func<ExchangeRateSnapshot> _next;

    public FakeExchangeRateProvider(Func<ExchangeRateSnapshot> next) => _next = next;

    public int Calls { get; private set; }

    public Task<ExchangeRateSnapshot> GetLatestRates(string baseCurrency, IReadOnlyCollection<string> currencies, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(_next());
    }
}
