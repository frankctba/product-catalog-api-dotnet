namespace Demo.Application.Common
{
    public class ExchangeRateUnavailableException : Exception
    {
        public ExchangeRateUnavailableException(string baseCurrency, string targetCurrency)
            : base($"No exchange rate from {baseCurrency} to {targetCurrency} is available yet. Try again later.")
        {
        }
    }
}
