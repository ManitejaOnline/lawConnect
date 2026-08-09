using LawConnect.Domain.Events;

namespace LawConnect.Domain.Common;

/// <summary>
/// Base type for persisted domain entities with audit, soft-delete, and domain-event support.
/// </summary>
public abstract class BaseEntity : IAuditableEntity, ISoftDeletable
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public string? UpdatedBy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the optimistic concurrency token.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Gets the domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to be dispatched after persistence.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Removes a previously added domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

    /// <summary>
    /// Clears all pending domain events.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
