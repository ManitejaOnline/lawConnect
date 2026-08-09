using FluentValidation;
using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.DeactivateDevice;

/// <summary>
/// Deactivates a device owned by the current user.
/// </summary>
public sealed record DeactivateDeviceCommand(string DeviceIdentifier) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeactivateDeviceCommand"/>.
/// </summary>
public sealed class DeactivateDeviceCommandValidator : AbstractValidator<DeactivateDeviceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeactivateDeviceCommandValidator"/> class.
    /// </summary>
    public DeactivateDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceIdentifier).NotEmpty().MaximumLength(256);
    }
}
