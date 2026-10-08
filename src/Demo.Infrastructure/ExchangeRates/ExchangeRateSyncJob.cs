using Demo.Application.Common;
using Demo.Application.Modules;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Demo.Infrastructure.ExchangeRates;

/// <summary>
/// Hangfire entry point for the exchange-rate sync. The logic lives in <see cref="IExchangeRateSyncModule"/>.
/// </summary>
public class ExchangeRateSyncJob
{
    public const string RecurringJobId = "exchange-rate-sync";

    private readonly IExchangeRateSyncModule _exchangeRateSyncModule;
    private readonly ILogger<ExchangeRateSyncJob> _logger;

    public ExchangeRateSyncJob(IExchangeRateSyncModule exchangeRateSyncModule, ILogger<ExchangeRateSyncJob> logger)
    {
        _exchangeRateSyncModule = exchangeRateSyncModule;
        _logger = logger;
    }

    // Permanent failures (bad configuration, unexpected provider data) fail at once; transient ones are retried.
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 3, ExceptOn = new[] { typeof(ExchangeRateSyncException) })]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _exchangeRateSyncModule.SyncLatestRatesAsync(cancellationToken);
        }
        catch (ExchangeRateSyncException ex)
        {
            // Hangfire only logs failures it is going to retry, so log the permanent ones here.
            _logger.LogError(ex, "Exchange-rate sync failed and will not be retried: {Reason}", ex.Message);
            throw;
        }
    }
}
