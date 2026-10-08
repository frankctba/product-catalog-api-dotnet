using Demo.Application.Common;
using Demo.Application.Modules;
using Demo.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Demo.Application
{
    public static class ApplicationSetup
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<CurrencyOptions>()
                .Bind(configuration.GetSection(CurrencyOptions.SectionName))
                .Validate(o => CurrencyCodes.IsValid(o.BaseCurrency),
                    $"{CurrencyOptions.SectionName}:BaseCurrency must be a 3-letter uppercase ISO 4217 code.")
                .Validate(o => o.SupportedCurrencies.All(CurrencyCodes.IsValid),
                    $"{CurrencyOptions.SectionName}:SupportedCurrencies must only contain 3-letter uppercase ISO 4217 codes.")
                .Validate(o => !o.SupportedCurrencies.Contains(o.BaseCurrency),
                    $"{CurrencyOptions.SectionName}:SupportedCurrencies must not contain the base currency.")
                .ValidateOnStart();

            // The system clock, replaceable in tests.
            services.TryAddSingleton(TimeProvider.System);

            // Scoped: one instance per HTTP request or Hangfire job, matching the DbContext the repositories use.
            services.AddScoped<IInventoryModule, InventoryModule>();
            services.AddScoped<IExchangeRateSyncModule, ExchangeRateSyncModule>();

            return services;
        }
    }
}
