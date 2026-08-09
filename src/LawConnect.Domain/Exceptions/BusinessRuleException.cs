namespace LawConnect.Domain.Exceptions;

/// <summary>
/// Thrown when a domain business rule is violated.
/// </summary>
public sealed class BusinessRuleException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="errorCode">Optional machine-readable error code.</param>
    public BusinessRuleException(string message, string? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the optional machine-readable error code.
    /// </summary>
    public string? ErrorCode { get; }
}
