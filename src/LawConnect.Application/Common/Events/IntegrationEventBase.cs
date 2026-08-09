namespace LawConnect.Application.Common.Events;

/// <summary>
/// Base type for integration events.
/// </summary>
public abstract class IntegrationEventBase : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    /// <inheritdoc />
    public abstract string EventType { get; }
}
