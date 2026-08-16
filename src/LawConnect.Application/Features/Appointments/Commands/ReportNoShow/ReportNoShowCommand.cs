using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using MediatR;

namespace LawConnect.Application.Features.Appointments.Commands.ReportNoShow;

/// <summary>
/// Customer-reported signal that the lawyer did not attend a scheduled appointment. Feeds the
/// Appointment Reliability component of the lawyer's Trust Score.
/// </summary>
public sealed record ReportNoShowCommand(Guid AppointmentId) : IRequest<Result<AppointmentResponse>>;
