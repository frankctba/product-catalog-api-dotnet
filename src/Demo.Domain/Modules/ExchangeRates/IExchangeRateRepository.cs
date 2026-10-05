namespace Demo.Domain.Modules.ExchangeRates
{
    public interface IExchangeRateRepository
    {
        /// <summary>
        /// Appends the snapshot to the history and updates the latest rates in one transaction.
        /// Returns false when the same snapshot (provider, base, timestamp) is already stored.
        /// </summary>
        Task<bool> SaveSnapshot(ExchangeRateSnapshot snapshot, CancellationToken cancellationToken);

        Task<IReadOnlyList<LatestExchangeRate>> GetLatestRates(string baseCurrency, CancellationToken cancellationToken);
    }
}
