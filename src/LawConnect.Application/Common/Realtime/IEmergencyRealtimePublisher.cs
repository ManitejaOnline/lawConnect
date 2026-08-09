using LawConnect.Application.Features.Emergency.Models;
using LawConnect.Domain.Enums;

namespace LawConnect.Application.Common.Realtime;

/// <summary>
/// Publishes emergency SOS realtime events over SignalR.
/// </summary>
public interface IEmergencyRealtimePublisher
{
    /// <summary>Pushes status updates to the customer and request group.</summary>
    Task PublishStatusAsync(EmergencyRequestResponse request, CancellationToken cancellationToken = default);

    /// <summary>Pushes an incoming SOS to a lawyer.</summary>
    Task PublishIncomingSosAsync(
        Guid lawyerUserId,
        IncomingEmergencySosResponse payload,
        CancellationToken cancellationToken = default);

    /// <summary>Pushes countdown ticks for a request.</summary>
    Task PublishCountdownAsync(
        Guid requestId,
        Guid customerUserId,
        int attemptNumber,
        DateTime expiresAtUtc,
        int remainingSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>Notifies lawyers that their pending assignment was cancelled.</summary>
    Task PublishAssignmentCancelledAsync(
        Guid lawyerUserId,
        Guid requestId,
        Guid assignmentId,
        string reason,
        CancellationToken cancellationToken = default);
}
