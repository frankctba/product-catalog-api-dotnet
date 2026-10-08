namespace Demo.Application.Common
{
    /// <summary>
    /// An exchange-rate sync failure that retrying will not fix: missing or rejected configuration,
    /// or provider data that does not match what was requested. Not retried by the background job.
    /// </summary>
    public class ExchangeRateSyncException : Exception
    {
        public ExchangeRateSyncException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
