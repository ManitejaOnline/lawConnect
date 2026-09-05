using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Commands.CompleteAppointment;

/// <summary>
/// Marks a scheduled appointment as completed. Only the assigned lawyer may call this after the slot start.
/// </summary>
public sealed record CompleteAppointmentCommand(Guid AppointmentId) : IRequest<Result<AppointmentResponse>>;

/// <summary>
/// Validates <see cref="CompleteAppointmentCommand"/>.
/// </summary>
public sealed class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteAppointmentCommandValidator"/> class.
    /// </summary>
    public CompleteAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}
