using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Commands.RescheduleAppointment;

/// <summary>
/// Reschedules an existing appointment to a new slot.
/// </summary>
public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateTime StartUtc,
    DateTime EndUtc,
    string TimeZoneId) : IRequest<Result<AppointmentResponse>>;

/// <summary>
/// Validates <see cref="RescheduleAppointmentCommand"/>.
/// </summary>
public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RescheduleAppointmentCommandValidator"/> class.
    /// </summary>
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.StartUtc).NotEmpty();
        RuleFor(x => x.EndUtc).NotEmpty();
        RuleFor(x => x).Must(x => x.EndUtc > x.StartUtc).WithMessage("EndUtc must be after StartUtc.");
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);
    }
}
