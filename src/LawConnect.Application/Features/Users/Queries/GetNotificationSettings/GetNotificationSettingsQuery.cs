using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Queries.GetNotificationSettings;

/// <summary>
/// Returns notification settings for the current user.
/// </summary>
public sealed record GetNotificationSettingsQuery : IRequest<Result<NotificationSettingsResponse>>;
