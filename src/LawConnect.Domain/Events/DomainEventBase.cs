namespace LawConnect.Domain.Events;

/// <summary>
/// Base type for domain events.
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
