namespace LawConnect.Domain.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with the current resource state.
/// </summary>
public sealed class ConflictException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public ConflictException(string message)
        : base(message)
    {
    }
}
