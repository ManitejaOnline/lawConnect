using LawConnect.Domain.Events;

namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Dispatches domain events to in-process handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches the provided domain events.
    /// </summary>
    /// <param name="domainEvents">Events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
