using Demo.Application.Common;
using Demo.Application.Modules;
using Demo.UnitTests.TestData;
using Demo.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace Demo.UnitTests.Application.Modules;

public class InventoryModuleTests
{
    private static readonly DateTime Monday = SampleData.Monday;

    private readonly FakeExchangeRateRepository _exchangeRates = new();
    private readonly InventoryModule _module;

    public InventoryModuleTests()
    {
        _module = new InventoryModule(
            NullLogger<InventoryModule>.Instance,
            new FakeProductRepository(SampleData.Products()),
            _exchangeRates,
            SampleData.CurrencyOptions());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("USD")]
    [InlineData("usd")]
    public async Task GetProducts_WithoutConversion_ReturnsBasePrices(string? currency)
    {
        var result = await _module.GetProductsAsync(currency, 1, 50, CancellationToken.None);

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
        await _exchangeRates.SaveSnapshotAsync(SampleData.Snapshot(Monday.AddDays(-7), ("EUR", 0.9184M)), CancellationToken.None);
        await _exchangeRates.SaveSnapshotAsync(SampleData.Snapshot(Monday, ("EUR", 0.9201M)), CancellationToken.None);

        var result = await _module.GetProductsAsync(currency, 1, 50, CancellationToken.None);

        Assert.All(result.Items, p => Assert.Equal("EUR", p.Currency));
        Assert.Equal([95.05M, 94.03M, 55.20M], result.Items.Select(p => p.Price));
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("XYZ")]
    public async Task GetProducts_WithUnsupportedCurrency_Throws(string currency)
    {
        var exception = await Assert.ThrowsAsync<UnsupportedCurrencyException>(
            () => _module.GetProductsAsync(currency, 1, 50, CancellationToken.None));

        Assert.Contains("USD, EUR, CAD, GBP, CHF", exception.Message);
    }

    [Fact]
    public async Task GetProducts_WithSupportedCurrencyButNoRate_Throws()
    {
        await Assert.ThrowsAsync<ExchangeRateUnavailableException>(
            () => _module.GetProductsAsync("GBP", 1, 50, CancellationToken.None));
    }

    [Fact]
    public async Task GetProducts_ReturnsRequestedPage()
    {
        var result = await _module.GetProductsAsync(null, 2, 2, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal("SKU3", Assert.Single(result.Items).Sku);
    }

    [Fact]
    public async Task GetProduct_ReturnsDtoWithBaseCurrency_OrNullWhenUnknown()
    {
        var product = await _module.GetProductAsync("SKU1", CancellationToken.None);

        Assert.Equal(new ProductDto("SKU1", "Classic Leather Jacket", 103.30M, "USD"), product);
        Assert.Null(await _module.GetProductAsync("NOPE", CancellationToken.None));
    }
}
