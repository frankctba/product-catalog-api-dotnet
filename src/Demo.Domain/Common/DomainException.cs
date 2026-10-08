namespace Demo.Domain.Common
{
    /// <summary>
    /// Thrown when an operation would break a domain invariant (e.g. a negative price or a non-positive exchange rate).
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }
    }
}
