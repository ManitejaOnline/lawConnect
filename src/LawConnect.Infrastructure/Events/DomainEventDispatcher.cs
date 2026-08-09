using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Events;

/// <summary>
/// MediatR notification wrapping a domain event.
/// </summary>
/// <typeparam name="TDomainEvent">Domain event type.</typeparam>
public sealed class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventNotification{TDomainEvent}"/> class.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }

    /// <summary>
    /// Gets the domain event payload.
    /// </summary>
    public TDomainEvent DomainEvent { get; }
}

/// <summary>
/// Dispatches domain events to MediatR handlers after persistence.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="publisher">MediatR publisher.</param>
    /// <param name="logger">Logger instance.</param>
    public DomainEventDispatcher(IPublisher publisher, ILogger<DomainEventDispatcher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            _logger.LogInformation(
                "Dispatching domain event {EventType} ({EventId})",
                domainEvent.GetType().Name,
                domainEvent.EventId);

            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent)
                ?? throw new InvalidOperationException($"Could not create notification for {domainEvent.GetType().Name}.");

            await _publisher.Publish((INotification)notification, cancellationToken);
        }
    }
}
