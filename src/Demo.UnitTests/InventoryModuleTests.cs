using Demo.Application.Common;
using Demo.Application.Modules;
using Microsoft.Extensions.Logging.Abstractions;

namespace Demo.UnitTests;

public class InventoryModuleTests
{
    private static readonly DateTime Monday = new(2026, 9, 28, 5, 0, 0, DateTimeKind.Utc);

    private readonly FakeExchangeRateRepository _exchangeRates = new();
    private readonly InventoryModule _module;

    public InventoryModuleTests()
    {
        _module = new InventoryModule(
            NullLogger<InventoryModule>.Instance,
            new FakeProductRepository(TestData.Products()),
            _exchangeRates,
            TestData.CurrencyOptions());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("USD")]
    [InlineData("usd")]
    public async Task GetProducts_WithoutConversion_ReturnsBasePrices(string? currency)
    {
        var result = await _module.GetProducts(currency, 1, 50, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, p => Assert.Equal("USD", p.Currency));
        Assert.Equal(103.30M, result.Items[0].Price);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("eur")]
    [InlineData(" Eur ")]
    public async Task GetProducts_WithSupportedCurrency_ConvertsWithLatestRate(string currency)
    {
        await _exchangeRates.SaveSnapshot(TestData.Snapshot(Monday.AddDays(-7), ("EUR", 0.9184M)), CancellationToken.None);
        await _exchangeRates.SaveSnapshot(TestData.Snapshot(Monday, ("EUR", 0.9201M)), CancellationToken.None);

        var result = await _module.GetProducts(currency, 1, 50, CancellationToken.None);

        Assert.All(result.Items, p => Assert.Equal("EUR", p.Currency));
        Assert.Equal([95.05M, 94.03M, 55.20M], result.Items.Select(p => p.Price));
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("XYZ")]
    public async Task GetProducts_WithUnsupportedCurrency_Throws(string currency)
    {
        var exception = await Assert.ThrowsAsync<UnsupportedCurrencyException>(
            () => _module.GetProducts(currency, 1, 50, CancellationToken.None));

        Assert.Contains("USD, EUR, CAD, GBP, CHF", exception.Message);
    }

    [Fact]
    public async Task GetProducts_WithSupportedCurrencyButNoRate_Throws()
    {
        await Assert.ThrowsAsync<ExchangeRateUnavailableException>(
            () => _module.GetProducts("GBP", 1, 50, CancellationToken.None));
    }

    [Fact]
    public async Task GetProducts_ReturnsRequestedPage()
    {
        var result = await _module.GetProducts(null, 2, 2, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal("SKU3", Assert.Single(result.Items).Sku);
    }

    [Fact]
    public async Task GetProduct_ReturnsDtoWithBaseCurrency_OrNullWhenUnknown()
    {
        var product = await _module.GetProduct("SKU1");

        Assert.Equal(new ProductDto("SKU1", "Classic Leather Jacket", 103.30M, "USD"), product);
        Assert.Null(await _module.GetProduct("NOPE"));
    }
}
