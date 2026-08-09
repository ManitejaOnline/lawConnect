namespace LawConnect.Application.Common.Models;

/// <summary>
/// Represents a structured application error.
/// </summary>
/// <param name="Code">Machine-readable error code.</param>
/// <param name="Message">Human-readable error message.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Gets an empty error used for successful results.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error Validation(string message) => new("Validation.Error", message);

    /// <summary>
    /// Creates a not-found error.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string message) => new("NotFound", message);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error Conflict(string message) => new("Conflict", message);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error Unauthorized(string message = "Unauthorized.") => new("Unauthorized", message);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error Forbidden(string message = "Forbidden.") => new("Forbidden", message);

    /// <summary>
    /// Creates a generic failure error.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error Failure(string code, string message) => new(code, message);
}
