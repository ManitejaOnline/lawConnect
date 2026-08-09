using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Commands.BookAppointment;

/// <summary>
/// Books an appointment with a lawyer.
/// </summary>
public sealed record BookAppointmentCommand(
    Guid LawyerId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Notes,
    string TimeZoneId) : IRequest<Result<AppointmentResponse>>;

/// <summary>
/// Validates <see cref="BookAppointmentCommand"/>.
/// </summary>
public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookAppointmentCommandValidator"/> class.
    /// </summary>
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.LawyerId).NotEmpty();
        RuleFor(x => x.StartUtc).NotEmpty();
        RuleFor(x => x.EndUtc).NotEmpty();
        RuleFor(x => x).Must(x => x.EndUtc > x.StartUtc).WithMessage("EndUtc must be after StartUtc.");
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Notes));
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);
    }
}
