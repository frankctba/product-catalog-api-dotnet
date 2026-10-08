namespace Demo.Application.Common
{
    public class UnsupportedCurrencyException : Exception
    {
        public UnsupportedCurrencyException(string currency, IEnumerable<string> supportedCurrencies)
            : base($"Currency '{currency}' is not supported. Supported currencies: {string.Join(", ", supportedCurrencies)}.")
        {
            Currency = currency;
        }

        public string Currency { get; }
    }
}
