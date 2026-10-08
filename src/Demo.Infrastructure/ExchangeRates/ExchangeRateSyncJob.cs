using Demo.Application.Modules;
using Hangfire;

namespace Demo.Infrastructure.ExchangeRates;

/// <summary>
/// Hangfire entry point for the exchange-rate sync. The logic lives in <see cref="IExchangeRateSyncModule"/>.
/// </summary>
public class ExchangeRateSyncJob
{
    public const string RecurringJobId = "exchange-rate-sync";

    private readonly IExchangeRateSyncModule _exchangeRateSyncModule;

    public ExchangeRateSyncJob(IExchangeRateSyncModule exchangeRateSyncModule)
    {
        _exchangeRateSyncModule = exchangeRateSyncModule;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 3)]
    public Task RunAsync(CancellationToken cancellationToken)
    {
        return _exchangeRateSyncModule.SyncLatestRatesAsync(cancellationToken);
    }
}
