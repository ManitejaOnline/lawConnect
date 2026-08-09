using LawConnect.Api.Extensions;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Models;
using LawConnect.Application.Features.Notifications.Commands.MarkNotificationRead;
using LawConnect.Application.Features.Notifications.Queries.GetMyNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// In-app notification history endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationsController"/> class.
    /// </summary>
    public NotificationsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Lists notifications for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetMyNotificationsQuery(pageNumber, pageSize, unreadOnly),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkNotificationReadCommand(notificationId), cancellationToken);
        return result.ToActionResult();
    }
}
