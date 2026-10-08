using Demo.Application.Modules;
using Demo.Domain.Modules.ExchangeRates;
using Demo.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace Demo.UnitTests.Application.Modules;

public class ExchangeRateSyncModuleTests
{
    private static readonly DateTime Monday = new(2026, 9, 28, 5, 0, 0, DateTimeKind.Utc);

    private readonly FakeExchangeRateRepository _repository = new();

    private ExchangeRateSyncModule CreateModule(Func<ExchangeRateSnapshot> providerResponse) =>
        new(NullLogger<ExchangeRateSyncModule>.Instance,
            new FakeExchangeRateProvider(providerResponse),
            _repository,
            TestData.CurrencyOptions());

    private static ExchangeRateSnapshot AllRates(DateTime timestamp) =>
        TestData.Snapshot(timestamp, ("EUR", 0.9201M), ("CAD", 1.3816M), ("GBP", 0.7905M), ("CHF", 0.8641M));

    [Fact]
    public async Task SyncLatestRates_StoresHistoryAndLatestRates()
    {
        var module = CreateModule(() => AllRates(Monday));

        await module.SyncLatestRates(CancellationToken.None);

        Assert.Single(_repository.Snapshots);
        Assert.Equal(4, _repository.Latest.Count);
        Assert.True(await module.HasLatestRates(CancellationToken.None));
    }

    [Fact]
    public async Task SyncLatestRates_RunTwiceForSamePublication_StoresItOnce()
    {
        var module = CreateModule(() => AllRates(Monday));

        await module.SyncLatestRates(CancellationToken.None);
        await module.SyncLatestRates(CancellationToken.None);

        Assert.Single(_repository.Snapshots);
    }

    [Fact]
    public async Task SyncLatestRates_NewerPublication_KeepsHistoryAndUpdatesLatest()
    {
        await CreateModule(() => AllRates(Monday.AddDays(-7))).SyncLatestRates(CancellationToken.None);

        var newer = AllRates(Monday);
        newer.Rates.Single(r => r.QuoteCurrency == "EUR").Rate = 0.9300M;
        await CreateModule(() => newer).SyncLatestRates(CancellationToken.None);

        Assert.Equal(2, _repository.Snapshots.Count);
        Assert.Equal(0.9300M, _repository.Latest.Single(r => r.QuoteCurrency == "EUR").Rate);
    }

    [Fact]
    public async Task SyncLatestRates_DropsCurrenciesThatAreNotConfigured()
    {
        var response = AllRates(Monday);
        response.Rates.Add(new ExchangeRate { QuoteCurrency = "JPY", Rate = 147.5M });

        await CreateModule(() => response).SyncLatestRates(CancellationToken.None);

        Assert.DoesNotContain(_repository.Latest, r => r.QuoteCurrency == "JPY");
    }

    [Fact]
    public async Task SyncLatestRates_MissingCurrency_ThrowsAndStoresNothing()
    {
        var module = CreateModule(() => TestData.Snapshot(Monday, ("EUR", 0.9201M), ("CAD", 1.3816M)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => module.SyncLatestRates(CancellationToken.None));

        Assert.Contains("GBP, CHF", exception.Message);
        Assert.Empty(_repository.Snapshots);
        Assert.False(await module.HasLatestRates(CancellationToken.None));
    }

    [Fact]
    public async Task SyncLatestRates_NonPositiveRate_ThrowsAndStoresNothing()
    {
        var response = AllRates(Monday);
        response.Rates.Single(r => r.QuoteCurrency == "GBP").Rate = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateModule(() => response).SyncLatestRates(CancellationToken.None));

        Assert.Empty(_repository.Snapshots);
    }

    [Fact]
    public async Task SyncLatestRates_UnexpectedBaseCurrency_ThrowsAndStoresNothing()
    {
        var response = AllRates(Monday);
        response.BaseCurrency = "EUR";

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateModule(() => response).SyncLatestRates(CancellationToken.None));

        Assert.Empty(_repository.Snapshots);
    }
}
