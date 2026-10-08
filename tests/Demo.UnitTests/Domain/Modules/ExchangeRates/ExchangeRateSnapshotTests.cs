using Demo.Domain.Common;
using Demo.Domain.Modules.ExchangeRates;
using Demo.UnitTests.TestData;

namespace Demo.UnitTests.Domain.Modules.ExchangeRates;

public class ExchangeRateSnapshotTests
{
    private static readonly DateTime Monday = SampleData.Monday;

    private static ExchangeRateSnapshot Create(
        string provider = "OpenExchangeRates",
        string baseCurrency = "USD",
        DateTime? rateTimestampUtc = null,
        Dictionary<string, decimal>? rates = null) =>
        ExchangeRateSnapshot.Create(
            provider,
            baseCurrency,
            rateTimestampUtc ?? Monday,
            Monday.AddHours(1),
            rates ?? new Dictionary<string, decimal> { ["EUR"] = 0.9201M, ["GBP"] = 0.7905M });

    [Fact]
    public void Create_WithValidValues_CreatesSnapshotWithRates()
    {
        var snapshot = Create();

        Assert.Equal("USD", snapshot.BaseCurrency);
        Assert.Equal(Monday, snapshot.RateTimestampUtc);
        Assert.Equal(["EUR", "GBP"], snapshot.Rates.Select(r => r.QuoteCurrency));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.5")]
    public void Create_WithNonPositiveRate_Throws(string rate)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Create(rates: new() { ["EUR"] = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture) }));

        Assert.Contains("EUR", exception.Message);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    public void Create_WithInvalidBaseCurrency_Throws(string baseCurrency)
    {
        Assert.Throws<DomainException>(() => Create(baseCurrency: baseCurrency));
    }

    [Fact]
    public void Create_WithInvalidQuoteCurrency_Throws()
    {
        Assert.Throws<DomainException>(() => Create(rates: new() { ["eur"] = 0.92M }));
    }

    [Fact]
    public void Create_WithRateForTheBaseCurrency_Throws()
    {
        Assert.Throws<DomainException>(() => Create(rates: new() { ["USD"] = 1M }));
    }

    [Fact]
    public void Create_WithoutRates_Throws()
    {
        Assert.Throws<DomainException>(() => Create(rates: []));
    }

    [Fact]
    public void Create_WithoutProvider_Throws()
    {
        Assert.Throws<DomainException>(() => Create(provider: " "));
    }

    [Fact]
    public void Create_WithNonUtcTimestamp_Throws()
    {
        Assert.Throws<DomainException>(() => Create(rateTimestampUtc: DateTime.SpecifyKind(Monday, DateTimeKind.Local)));
    }
}
