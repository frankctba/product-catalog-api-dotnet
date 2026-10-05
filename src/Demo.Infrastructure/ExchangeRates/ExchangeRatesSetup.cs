using Demo.Application.Common.Services;
using Demo.Application.Modules;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Demo.Infrastructure.ExchangeRates;

public static class ExchangeRatesSetup
{
    public static IServiceCollection AddInfrastructureExchangeRates(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenExchangeRatesOptions>(configuration.GetSection(OpenExchangeRatesOptions.SectionName));
        services.Configure<ExchangeRateSyncOptions>(configuration.GetSection(ExchangeRateSyncOptions.SectionName));

        services.AddHttpClient<IExchangeRateProvider, OpenExchangeRatesClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenExchangeRatesOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddTransient<ExchangeRateSyncJob>();

        return services;
    }

    /// <summary>
    /// Registers the recurring sync and, when no rates are stored yet, enqueues one sync right away.
    /// </summary>
    public static async Task ScheduleExchangeRateSync(this IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ExchangeRatesSetup));
        var syncOptions = serviceProvider.GetRequiredService<IOptions<ExchangeRateSyncOptions>>().Value;
        var providerOptions = serviceProvider.GetRequiredService<IOptions<OpenExchangeRatesOptions>>().Value;

        // AddOrUpdate runs on every startup, so the schedule survives restarts even with in-memory storage.
        serviceProvider.GetRequiredService<IRecurringJobManager>().AddOrUpdate<ExchangeRateSyncJob>(
            ExchangeRateSyncJob.RecurringJobId,
            job => job.Run(CancellationToken.None),
            syncOptions.Cron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        if (!syncOptions.SyncOnStartupWhenMissing)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(providerOptions.AppId))
        {
            logger.LogWarning(
                "{Section}:AppId is not configured: skipping the startup exchange-rate sync. Currency conversion returns 503 until rates are stored.",
                OpenExchangeRatesOptions.SectionName);
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var syncModule = scope.ServiceProvider.GetRequiredService<IExchangeRateSyncModule>();

        if (!await syncModule.HasLatestRates(CancellationToken.None))
        {
            logger.LogInformation("No exchange rates stored yet: enqueuing a sync now");
            serviceProvider.GetRequiredService<IBackgroundJobClient>()
                .Enqueue<ExchangeRateSyncJob>(job => job.Run(CancellationToken.None));
        }
    }
}
