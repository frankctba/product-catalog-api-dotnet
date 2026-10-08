using Demo.Application.Common.Services;
using Demo.Domain.Modules.ExchangeRates;

namespace Demo.UnitTests.TestDoubles;

internal class FakeExchangeRateProvider : IExchangeRateProvider
{
    private readonly Func<ExchangeRateSnapshot> _next;

    public FakeExchangeRateProvider(Func<ExchangeRateSnapshot> next) => _next = next;

    public int Calls { get; private set; }

    public Task<ExchangeRateSnapshot> GetLatestRatesAsync(string baseCurrency, IReadOnlyCollection<string> currencies, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(_next());
    }
}
