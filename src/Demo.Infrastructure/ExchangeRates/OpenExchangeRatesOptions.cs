namespace Demo.Infrastructure.ExchangeRates;

public class OpenExchangeRatesOptions
{
    public const string SectionName = "OpenExchangeRates";

    public string BaseUrl { get; set; } = "https://openexchangerates.org/api/";

    /// <summary>Secret. Set with: dotnet user-secrets set "OpenExchangeRates:AppId" "your-app-id"</summary>
    public string AppId { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}
