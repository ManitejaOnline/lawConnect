namespace LawConnect.Domain.Exceptions;

/// <summary>
/// Thrown when authentication is required or invalid.
/// </summary>
public sealed class UnauthorizedException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public UnauthorizedException(string message = "Unauthorized.")
        : base(message)
    {
    }
}
