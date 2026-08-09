using LawConnect.Application.Common.Events;
using MediatR;

namespace LawConnect.Infrastructure.Events;

/// <summary>
/// MediatR notification wrapping an integration event for in-process handlers.
/// </summary>
public sealed class IntegrationEventNotification : INotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEventNotification"/> class.
    /// </summary>
    /// <param name="integrationEvent">The integration event to publish.</param>
    public IntegrationEventNotification(IIntegrationEvent integrationEvent)
    {
        IntegrationEvent = integrationEvent;
    }

    /// <summary>
    /// Gets the integration event payload.
    /// </summary>
    public IIntegrationEvent IntegrationEvent { get; }
}
