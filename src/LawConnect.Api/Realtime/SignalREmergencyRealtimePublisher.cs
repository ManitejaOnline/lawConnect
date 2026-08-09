using LawConnect.Api.Hubs;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Emergency.Models;
using Microsoft.AspNetCore.SignalR;

namespace LawConnect.Api.Realtime;

/// <summary>
/// SignalR publisher for emergency SOS events.
/// </summary>
public sealed class SignalREmergencyRealtimePublisher : IEmergencyRealtimePublisher
{
    private readonly IHubContext<EmergencyHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalREmergencyRealtimePublisher"/> class.
    /// </summary>
    public SignalREmergencyRealtimePublisher(IHubContext<EmergencyHub> hubContext) =>
        _hubContext = hubContext;

    /// <inheritdoc />
    public Task PublishStatusAsync(EmergencyRequestResponse request, CancellationToken cancellationToken = default) =>
        Task.WhenAll(
            _hubContext.Clients.Group(EmergencyHub.RequestGroup(request.RequestId))
                .SendAsync("SosStatusChanged", request, cancellationToken),
            _hubContext.Clients.Group(EmergencyHub.UserGroup(request.CustomerUserId))
                .SendAsync("SosStatusChanged", request, cancellationToken));

    /// <inheritdoc />
    public Task PublishIncomingSosAsync(
        Guid lawyerUserId,
        IncomingEmergencySosResponse payload,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(EmergencyHub.UserGroup(lawyerUserId))
            .SendAsync("IncomingSos", payload, cancellationToken);

    /// <inheritdoc />
    public Task PublishCountdownAsync(
        Guid requestId,
        Guid customerUserId,
        int attemptNumber,
        DateTime expiresAtUtc,
        int remainingSeconds,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            requestId,
            attemptNumber,
            expiresAtUtc,
            remainingSeconds
        };

        return Task.WhenAll(
            _hubContext.Clients.Group(EmergencyHub.RequestGroup(requestId))
                .SendAsync("SosCountdown", payload, cancellationToken),
            _hubContext.Clients.Group(EmergencyHub.UserGroup(customerUserId))
                .SendAsync("SosCountdown", payload, cancellationToken));
    }

    /// <inheritdoc />
    public Task PublishAssignmentCancelledAsync(
        Guid lawyerUserId,
        Guid requestId,
        Guid assignmentId,
        string reason,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(EmergencyHub.UserGroup(lawyerUserId))
            .SendAsync(
                "SosAssignmentCancelled",
                new { requestId, assignmentId, reason },
                cancellationToken);
}
