namespace LawConnect.Domain.Exceptions;

/// <summary>
/// Base exception for domain and application rule failures.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    protected DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
