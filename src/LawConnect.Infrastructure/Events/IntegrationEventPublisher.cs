using LawConnect.Application.Common.Events;
using LawConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LawConnect.Infrastructure.Events;

/// <summary>
/// Publishes integration events through MediatR for in-process subscribers.
/// External message-bus adapters can implement <see cref="IIntegrationEventPublisher"/> later without changing callers.
/// </summary>
public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEventPublisher"/> class.
    /// </summary>
    /// <param name="publisher">MediatR publisher.</param>
    /// <param name="logger">Logger instance.</param>
    public IntegrationEventPublisher(IPublisher publisher, ILogger<IntegrationEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        _logger.LogInformation(
            "Publishing integration event {EventType} ({EventId})",
            integrationEvent.EventType,
            integrationEvent.EventId);

        await _publisher.Publish(new IntegrationEventNotification(integrationEvent), cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        IEnumerable<IIntegrationEvent> integrationEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvents);

        foreach (var integrationEvent in integrationEvents)
        {
            await PublishAsync(integrationEvent, cancellationToken);
        }
    }
}
