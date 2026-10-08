using Demo.Domain.Common;
using Demo.Domain.Modules.ExchangeRates;
using Demo.UnitTests.TestData;

namespace Demo.UnitTests.Domain.Modules.ExchangeRates;

public class LatestExchangeRateTests
{
    private static readonly DateTime Monday = SampleData.Monday;
    private static readonly DateTime Now = Monday.AddDays(1);

    private static (ExchangeRateSnapshot Snapshot, ExchangeRate Rate) Eur(DateTime publishedAt, decimal rate)
    {
        var snapshot = SampleData.Snapshot(publishedAt, ("EUR", rate));
        return (snapshot, snapshot.Rates.Single());
    }

    [Fact]
    public void Create_TakesRateAndTimestampFromSnapshot()
    {
        var (snapshot, rate) = Eur(Monday, 0.9201M);

        var latest = LatestExchangeRate.Create(snapshot, rate, Now);

        Assert.Equal(("USD", "EUR", 0.9201M, Monday, Now),
            (latest.BaseCurrency, latest.QuoteCurrency, latest.Rate, latest.RateTimestampUtc, latest.UpdatedAtUtc));
    }

    [Fact]
    public void UpdateFrom_NewerPublication_UpdatesRate()
    {
        var (first, firstRate) = Eur(Monday.AddDays(-7), 0.9184M);
        var latest = LatestExchangeRate.Create(first, firstRate, Now);

        var (newer, newerRate) = Eur(Monday, 0.9201M);

        Assert.True(latest.UpdateFrom(newer, newerRate, Now));
        Assert.Equal(0.9201M, latest.Rate);
        Assert.Equal(Monday, latest.RateTimestampUtc);
    }

    [Theory]
    [InlineData(-7)] // older publication arriving late
    [InlineData(0)]  // same publication again
    public void UpdateFrom_OlderOrSamePublication_KeepsCurrentRate(int daysFromCurrent)
    {
        var (current, currentRate) = Eur(Monday, 0.9201M);
        var latest = LatestExchangeRate.Create(current, currentRate, Now);

        var (other, otherRate) = Eur(Monday.AddDays(daysFromCurrent), 0.9000M);

        Assert.False(latest.UpdateFrom(other, otherRate, Now));
        Assert.Equal(0.9201M, latest.Rate);
        Assert.Equal(Monday, latest.RateTimestampUtc);
    }

    [Fact]
    public void UpdateFrom_DifferentCurrencyPair_Throws()
    {
        var (snapshot, rate) = Eur(Monday, 0.9201M);
        var latest = LatestExchangeRate.Create(snapshot, rate, Now);

        var gbpSnapshot = SampleData.Snapshot(Monday.AddDays(7), ("GBP", 0.79M));

        Assert.Throws<DomainException>(() => latest.UpdateFrom(gbpSnapshot, gbpSnapshot.Rates.Single(), Now));
    }
}
