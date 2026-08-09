namespace LawConnect.Domain.Exceptions;

/// <summary>
/// Thrown when application or domain validation fails.
/// </summary>
public sealed class ValidationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="errors">Property validation errors.</param>
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the validation errors grouped by property name.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }
}
