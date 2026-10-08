using Demo.Application.Common;
using Demo.Application.Common.Services;
using Demo.Application.Modules;
using Demo.UnitTests.TestData;
using Demo.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace Demo.UnitTests.Application.Modules;

public class ExchangeRateSyncModuleTests
{
    private static readonly DateTime Monday = SampleData.Monday;

    private readonly FakeExchangeRateRepository _repository = new();

    private ExchangeRateSyncModule CreateModule(Func<ProviderRates> providerResponse) =>
        new(NullLogger<ExchangeRateSyncModule>.Instance,
            new FakeExchangeRateProvider(providerResponse),
            _repository,
            new FakeTimeProvider(Monday.AddHours(1)),
            SampleData.CurrencyOptions());

    [Fact]
    public async Task SyncLatestRatesAsync_StoresHistoryAndLatestRates()
    {
        var module = CreateModule(() => SampleData.AllProviderRates(Monday));

        await module.SyncLatestRatesAsync(CancellationToken.None);

        var snapshot = Assert.Single(_repository.Snapshots);
        Assert.Equal(Monday, snapshot.RateTimestampUtc);
        Assert.Equal(Monday.AddHours(1), snapshot.FetchedAtUtc);
        Assert.Equal(4, _repository.Latest.Count);
        Assert.True(await module.HasLatestRatesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SyncLatestRatesAsync_RunTwiceForSamePublication_StoresItOnce()
    {
        var module = CreateModule(() => SampleData.AllProviderRates(Monday));

        await module.SyncLatestRatesAsync(CancellationToken.None);
        await module.SyncLatestRatesAsync(CancellationToken.None);

        Assert.Single(_repository.Snapshots);
    }

    [Fact]
    public async Task SyncLatestRatesAsync_NewerPublication_KeepsHistoryAndUpdatesLatest()
    {
        await CreateModule(() => SampleData.AllProviderRates(Monday.AddDays(-7))).SyncLatestRatesAsync(CancellationToken.None);
        await CreateModule(() => SampleData.AllProviderRates(Monday, eur: 0.9300M)).SyncLatestRatesAsync(CancellationToken.None);

        Assert.Equal(2, _repository.Snapshots.Count);
        Assert.Equal(0.9300M, _repository.Latest.Single(r => r.QuoteCurrency == "EUR").Rate);
    }

    [Fact]
    public async Task SyncLatestRatesAsync_DropsCurrenciesThatAreNotConfigured()
    {
        var response = SampleData.ProviderRates(Monday, ("EUR", 0.9201M), ("CAD", 1.3816M), ("GBP", 0.7905M), ("CHF", 0.8641M), ("JPY", 147.5M));

        await CreateModule(() => response).SyncLatestRatesAsync(CancellationToken.None);

        Assert.DoesNotContain(_repository.Latest, r => r.QuoteCurrency == "JPY");
    }

    [Fact]
    public async Task SyncLatestRatesAsync_MissingCurrency_FailsPermanentlyAndStoresNothing()
    {
        var module = CreateModule(() => SampleData.ProviderRates(Monday, ("EUR", 0.9201M), ("CAD", 1.3816M)));

        var exception = await Assert.ThrowsAsync<ExchangeRateSyncException>(() => module.SyncLatestRatesAsync(CancellationToken.None));

        Assert.Contains("GBP, CHF", exception.Message);
        Assert.Empty(_repository.Snapshots);
        Assert.False(await module.HasLatestRatesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SyncLatestRatesAsync_NonPositiveRate_FailsPermanentlyAndStoresNothing()
    {
        var response = SampleData.ProviderRates(Monday, ("EUR", 0.9201M), ("CAD", 1.3816M), ("GBP", 0M), ("CHF", 0.8641M));

        var exception = await Assert.ThrowsAsync<ExchangeRateSyncException>(() => CreateModule(() => response).SyncLatestRatesAsync(CancellationToken.None));

        Assert.Contains("GBP", exception.Message);
        Assert.Empty(_repository.Snapshots);
    }

    [Fact]
    public async Task SyncLatestRatesAsync_UnexpectedBaseCurrency_FailsPermanentlyAndStoresNothing()
    {
        var response = SampleData.AllProviderRates(Monday) with { BaseCurrency = "EUR" };

        await Assert.ThrowsAsync<ExchangeRateSyncException>(() => CreateModule(() => response).SyncLatestRatesAsync(CancellationToken.None));

        Assert.Empty(_repository.Snapshots);
    }
}
