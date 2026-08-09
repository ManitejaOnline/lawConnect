using LawConnect.Application.Common.Events;

namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Publishes integration events to interested subscribers.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes a single integration event.
    /// </summary>
    /// <param name="integrationEvent">Event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple integration events.
    /// </summary>
    /// <param name="integrationEvents">Events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken = default);
}
