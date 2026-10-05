namespace Demo.Application.Modules
{
    public static class CurrencyConverter
    {
        /// <summary>All supported currencies (USD, EUR, CAD, GBP, CHF) use two decimal places.</summary>
        public const int DecimalPlaces = 2;

        /// <summary>
        /// Converts an amount with the given rate and rounds once, at the end, half away from zero.
        /// </summary>
        public static decimal Convert(decimal amount, decimal rate)
        {
            return Math.Round(amount * rate, DecimalPlaces, MidpointRounding.AwayFromZero);
        }
    }
}
