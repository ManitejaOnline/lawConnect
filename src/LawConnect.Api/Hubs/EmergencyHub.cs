using System.Security.Claims;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Features.Emergency.Commands.AcceptEmergencyAssignment;
using LawConnect.Application.Features.Emergency.Commands.DeclineEmergencyAssignment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Api.Hubs;

/// <summary>
/// Realtime hub for emergency SOS updates, incoming alerts, and countdown.
/// </summary>
[Authorize]
public sealed class EmergencyHub : Hub
{
    private readonly ISender _sender;
    private readonly IApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmergencyHub"/> class.
    /// </summary>
    public EmergencyHub(ISender sender, IApplicationDbContext dbContext)
    {
        _sender = sender;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Joins the SOS request group after ownership validation.
    /// </summary>
    public async Task JoinRequest(Guid requestId)
    {
        var userId = GetUserId();
        var allowed = await _dbContext.EmergencyRequests.AsNoTracking()
            .AnyAsync(r =>
                r.Id == requestId
                && (r.Customer.UserId == userId
                    || r.AssignedLawyerId != null
                       && _dbContext.Lawyers.Any(l => l.Id == r.AssignedLawyerId && l.UserId == userId)
                    || _dbContext.EmergencyAssignments.Any(a =>
                        a.EmergencyRequestId == requestId && a.Lawyer.UserId == userId)));

        if (!allowed)
        {
            throw new HubException("You are not allowed to join this SOS request.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RequestGroup(requestId));
    }

    /// <summary>
    /// Leaves the SOS request group.
    /// </summary>
    public Task LeaveRequest(Guid requestId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, RequestGroup(requestId));

    /// <summary>
    /// Accepts an incoming SOS assignment.
    /// </summary>
    public Task AcceptAssignment(Guid assignmentId) =>
        _sender.Send(new AcceptEmergencyAssignmentCommand(assignmentId));

    /// <summary>
    /// Declines an incoming SOS assignment.
    /// </summary>
    public Task DeclineAssignment(Guid assignmentId) =>
        _sender.Send(new DeclineEmergencyAssignmentCommand(assignmentId));

    private Guid GetUserId()
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id)
            ? id
            : throw new HubException("Authentication is required.");
    }

    internal static string RequestGroup(Guid requestId) => $"emergency:{requestId:D}";

    internal static string UserGroup(Guid userId) => $"emergency-user:{userId:D}";
}
