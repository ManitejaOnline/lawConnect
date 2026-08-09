using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.UpdateNotificationSettings;

/// <summary>
/// Updates notification settings for the current user.
/// </summary>
public sealed record UpdateNotificationSettingsCommand(
    bool EnablePush,
    bool EnableSms,
    bool EnableEmail,
    bool EnableInApp) : IRequest<Result<NotificationSettingsResponse>>;

/// <summary>
/// Validates <see cref="UpdateNotificationSettingsCommand"/>.
/// </summary>
public sealed class UpdateNotificationSettingsCommandValidator : AbstractValidator<UpdateNotificationSettingsCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateNotificationSettingsCommandValidator"/> class.
    /// </summary>
    public UpdateNotificationSettingsCommandValidator()
    {
    }
}
