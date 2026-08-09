namespace LawConnect.Application.Common.Events;

/// <summary>
/// Marker for cross-boundary integration events.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Gets the event type name used for routing and logging.
    /// </summary>
    string EventType { get; }
}
