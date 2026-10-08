namespace Demo.Domain.Common
{
    public static class CurrencyCodes
    {
        /// <summary>True for a 3-letter uppercase ISO 4217 code such as USD or EUR.</summary>
        public static bool IsValid(string? code) =>
            code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);

        public static void EnsureValid(string? code, string description)
        {
            if (!IsValid(code))
            {
                throw new DomainException($"{description} '{code}' is not a 3-letter uppercase ISO 4217 currency code.");
            }
        }
    }
}
