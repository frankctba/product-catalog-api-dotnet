using Demo.Application.Common;
using Demo.Application.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Application
{
    public static class ApplicationSetup
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<CurrencyOptions>()
                .Bind(configuration.GetSection(CurrencyOptions.SectionName))
                .Validate(o => IsCurrencyCode(o.BaseCurrency),
                    $"{CurrencyOptions.SectionName}:BaseCurrency must be a 3-letter uppercase ISO 4217 code.")
                .Validate(o => o.SupportedCurrencies.All(IsCurrencyCode),
                    $"{CurrencyOptions.SectionName}:SupportedCurrencies must only contain 3-letter uppercase ISO 4217 codes.")
                .Validate(o => !o.SupportedCurrencies.Contains(o.BaseCurrency),
                    $"{CurrencyOptions.SectionName}:SupportedCurrencies must not contain the base currency.")
                .ValidateOnStart();

            // Scoped: one instance per HTTP request or Hangfire job, matching the DbContext the repositories use.
            services.AddScoped<IInventoryModule, InventoryModule>();
            services.AddScoped<IExchangeRateSyncModule, ExchangeRateSyncModule>();

            return services;
        }

        private static bool IsCurrencyCode(string? code) =>
            code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);
    }
}
