using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Queries.GetAppointmentCalendar;

/// <summary>
/// Returns appointments in a date range for calendar views.
/// </summary>
public sealed record GetAppointmentCalendarQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IRequest<Result<IReadOnlyList<AppointmentResponse>>>;

/// <summary>
/// Validates <see cref="GetAppointmentCalendarQuery"/>.
/// </summary>
public sealed class GetAppointmentCalendarQueryValidator : AbstractValidator<GetAppointmentCalendarQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAppointmentCalendarQueryValidator"/> class.
    /// </summary>
    public GetAppointmentCalendarQueryValidator()
    {
        RuleFor(x => x.FromUtc).NotEmpty();
        RuleFor(x => x.ToUtc).NotEmpty();
        RuleFor(x => x).Must(x => x.ToUtc > x.FromUtc).WithMessage("ToUtc must be after FromUtc.");
        RuleFor(x => x).Must(x => (x.ToUtc - x.FromUtc) <= TimeSpan.FromDays(62))
            .WithMessage("Calendar range cannot exceed 62 days.");
    }
}
