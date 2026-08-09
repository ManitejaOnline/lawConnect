using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.UpdateDevice;

/// <summary>
/// Updates a device owned by the current user.
/// </summary>
public sealed record UpdateDeviceCommand(
    string DeviceIdentifier,
    DevicePlatform? Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion) : IRequest<Result<DeviceResponse>>;

/// <summary>
/// Validates <see cref="UpdateDeviceCommand"/>.
/// </summary>
public sealed class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDeviceCommandValidator"/> class.
    /// </summary>
    public UpdateDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceIdentifier).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Platform).IsInEnum().When(x => x.Platform.HasValue);
        RuleFor(x => x.FcmToken).MaximumLength(512).When(x => !string.IsNullOrWhiteSpace(x.FcmToken));
        RuleFor(x => x.AppVersion).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.AppVersion));
        RuleFor(x => x.OsVersion).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.OsVersion));
    }
}
