namespace LawConnect.Domain.Events;

/// <summary>
/// Marker for in-process domain events raised by aggregates.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }
}
