#pragma warning disable CS1591
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LawConnect.Application.Common.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LawConnect.Api.Hubs;

/// <summary>
/// Presence channel for phone-to-phone document transfer.
/// A customer counts as online only while this hub (or chat) has a live connection.
/// </summary>
[Authorize]
public sealed class DocumentHub : Hub
{
    private readonly IPresenceTracker _presence;

    public DocumentHub(IPresenceTracker presence) => _presence = presence;

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await _presence.UserConnectedAsync(GetUserId(), Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _presence.UserDisconnectedAsync(GetUserId(), Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(value ?? throw new HubException("Missing user id."));
    }
}
