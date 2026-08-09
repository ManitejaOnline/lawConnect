using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Commands.CancelAppointment;

/// <summary>
/// Cancels an appointment owned by the current participant.
/// </summary>
public sealed record CancelAppointmentCommand(
    Guid AppointmentId,
    string? Reason) : IRequest<Result<AppointmentResponse>>;

/// <summary>
/// Validates <see cref="CancelAppointmentCommand"/>.
/// </summary>
public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelAppointmentCommandValidator"/> class.
    /// </summary>
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
