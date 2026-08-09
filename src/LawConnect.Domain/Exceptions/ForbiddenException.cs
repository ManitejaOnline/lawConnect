namespace LawConnect.Domain.Exceptions;

/// <summary>
/// Thrown when the caller is authenticated but not allowed to perform the operation.
/// </summary>
public sealed class ForbiddenException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public ForbiddenException(string message = "Forbidden.")
        : base(message)
    {
    }
}
