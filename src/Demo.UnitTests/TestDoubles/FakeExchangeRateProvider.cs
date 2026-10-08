using Demo.Application.Common.Services;

namespace Demo.UnitTests.TestDoubles;

internal class FakeExchangeRateProvider : IExchangeRateProvider
{
    private readonly Func<ProviderRates> _next;

    public FakeExchangeRateProvider(Func<ProviderRates> next) => _next = next;

    public int Calls { get; private set; }

    public Task<ProviderRates> GetLatestRatesAsync(string baseCurrency, IReadOnlyCollection<string> currencies, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(_next());
    }
}
