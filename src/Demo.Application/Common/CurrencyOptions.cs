namespace Demo.Application.Common
{
    public class CurrencyOptions
    {
        public const string SectionName = "Currency";

        /// <summary>Currency product prices are stored in, and the base the exchange rates are fetched against.</summary>
        public string BaseCurrency { get; set; } = "USD";

        /// <summary>Currencies (besides the base) the catalog can be converted to and the sync job fetches.</summary>
        public List<string> SupportedCurrencies { get; set; } = [];
    }
}
