using System.Net;
using Demo.Application.Common;
using Demo.Application.Common.Services;
using Demo.Infrastructure.ExchangeRates;
using Demo.IntegrationTests.TestDoubles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Demo.IntegrationTests.Infrastructure.ExchangeRates;

public class OpenExchangeRatesClientTests
{
    // Shape of https://openexchangerates.org/api/latest.json; 1790571600 = 2026-09-28 05:00:00 UTC.
    private const string LatestJson = """
        { "disclaimer": "...", "license": "...", "timestamp": 1790571600, "base": "USD",
          "rates": { "EUR": 0.9201, "GBP": 0.7905 } }
        """;

    private const string InvalidAppIdJson = """
        { "error": true, "status": 401, "message": "invalid_app_id", "description": "Invalid App ID provided." }
        """;

    private static OpenExchangeRatesClient CreateClient(StubHttpMessageHandler handler, string appId = "test-key") =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://openexchangerates.org/api/") },
            Options.Create(new OpenExchangeRatesOptions { AppId = appId }));

    [Fact]
    public async Task GetLatestRatesAsync_SendsSymbolsAndKeyAsHeader_AndParsesTheResponse()
    {
        var handler = new StubHttpMessageHandler().Respond(HttpStatusCode.OK, LatestJson);

        var rates = await CreateClient(handler).GetLatestRatesAsync("USD", ["EUR", "GBP"], CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://openexchangerates.org/api/latest.json?symbols=EUR,GBP", request.RequestUri!.ToString());
        Assert.Equal("Token test-key", request.Headers.Authorization!.ToString());

        Assert.Equal("USD", rates.BaseCurrency);
        Assert.Equal(new DateTime(2026, 9, 28, 5, 0, 0, DateTimeKind.Utc), rates.RateTimestampUtc);
        Assert.Equal(DateTimeKind.Utc, rates.RateTimestampUtc.Kind);
        Assert.Equal(0.9201M, rates.Rates["EUR"]);
    }

    [Fact]
    public async Task GetLatestRatesAsync_WithNonUsdBase_SendsBaseParameter()
    {
        var handler = new StubHttpMessageHandler().Respond(HttpStatusCode.OK, LatestJson);

        await CreateClient(handler).GetLatestRatesAsync("EUR", ["USD"], CancellationToken.None);

        Assert.EndsWith("latest.json?symbols=USD&base=EUR", handler.Requests.Single().RequestUri!.ToString());
    }

    [Fact]
    public async Task GetLatestRatesAsync_WithoutAppId_FailsPermanentlyWithoutCallingTheProvider()
    {
        var handler = new StubHttpMessageHandler().Respond(HttpStatusCode.OK, LatestJson);

        await Assert.ThrowsAsync<ExchangeRateSyncException>(
            () => CreateClient(handler, appId: "").GetLatestRatesAsync("USD", ["EUR"], CancellationToken.None));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetLatestRatesAsync_RejectedKey_FailsPermanentlyWithTheProviderMessage()
    {
        var handler = new StubHttpMessageHandler().Respond(HttpStatusCode.Unauthorized, InvalidAppIdJson);

        var exception = await Assert.ThrowsAsync<ExchangeRateSyncException>(
            () => CreateClient(handler).GetLatestRatesAsync("USD", ["EUR"], CancellationToken.None));

        Assert.Contains("401: invalid_app_id Invalid App ID provided.", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task GetLatestRatesAsync_TransientStatus_ThrowsHttpRequestException(HttpStatusCode status)
    {
        var handler = new StubHttpMessageHandler().Respond(status, "{}");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateClient(handler).GetLatestRatesAsync("USD", ["EUR"], CancellationToken.None));

        Assert.Equal(status, exception.StatusCode);
    }

    // The tests below go through the real DI registration, including the resilience pipeline.

    private static (IExchangeRateProvider Provider, StubHttpMessageHandler Handler) CreateRegisteredClient()
    {
        var handler = new StubHttpMessageHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OpenExchangeRates:AppId"] = "test-key" })
            .Build();

        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructureExchangeRates(configuration);
        services.AddHttpClient<IExchangeRateProvider, OpenExchangeRatesClient>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        // No waiting between retries in tests. A typed client is named after its interface.
        services.Configure<HttpStandardResilienceOptions>(
            $"{nameof(IExchangeRateProvider)}-standard", o => o.Retry.Delay = TimeSpan.FromMilliseconds(1));

        return (services.BuildServiceProvider().GetRequiredService<IExchangeRateProvider>(), handler);
    }

    [Fact]
    public async Task RegisteredClient_RetriesServerErrors()
    {
        var (provider, handler) = CreateRegisteredClient();
        handler.Respond(HttpStatusCode.ServiceUnavailable, "{}").Respond(HttpStatusCode.OK, LatestJson);

        var rates = await provider.GetLatestRatesAsync("USD", ["EUR", "GBP"], CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(0.7905M, rates.Rates["GBP"]);
    }

    [Fact]
    public async Task RegisteredClient_DoesNotRetryARejectedKey()
    {
        var (provider, handler) = CreateRegisteredClient();
        handler.Respond(HttpStatusCode.Unauthorized, InvalidAppIdJson);

        await Assert.ThrowsAsync<ExchangeRateSyncException>(
            () => provider.GetLatestRatesAsync("USD", ["EUR"], CancellationToken.None));

        Assert.Single(handler.Requests);
    }
}
