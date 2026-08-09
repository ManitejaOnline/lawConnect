namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the system clock for testability.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    DateTime UtcNow { get; }
}
