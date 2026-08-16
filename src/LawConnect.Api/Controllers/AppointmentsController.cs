using LawConnect.Api.Extensions;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Commands.BookAppointment;
using LawConnect.Application.Features.Appointments.Commands.CancelAppointment;
using LawConnect.Application.Features.Appointments.Commands.ReportNoShow;
using LawConnect.Application.Features.Appointments.Commands.RescheduleAppointment;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Queries.GetAppointmentById;
using LawConnect.Application.Features.Appointments.Queries.GetAppointmentCalendar;
using LawConnect.Application.Features.Appointments.Queries.GetAppointmentHistory;
using LawConnect.Application.Features.Appointments.Queries.GetUpcomingAppointments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>
/// Appointment booking and calendar endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("api/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppointmentsController"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    public AppointmentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Books an appointment with a lawyer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(
        [FromBody] BookAppointmentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new BookAppointmentCommand(
                request.LawyerId,
                request.StartUtc,
                request.EndUtc,
                request.Notes,
                request.TimeZoneId),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Cancels an appointment.
    /// </summary>
    [HttpPost("{appointmentId:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid appointmentId,
        [FromBody] CancelAppointmentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CancelAppointmentCommand(appointmentId, request.Reason),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Reschedules an appointment.
    /// </summary>
    [HttpPost("{appointmentId:guid}/reschedule")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reschedule(
        Guid appointmentId,
        [FromBody] RescheduleAppointmentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RescheduleAppointmentCommand(
                appointmentId,
                request.StartUtc,
                request.EndUtc,
                request.TimeZoneId),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Reports that the lawyer did not attend a scheduled appointment. Feeds Trust Score.
    /// </summary>
    [HttpPost("{appointmentId:guid}/report-no-show")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportNoShow(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReportNoShowCommand(appointmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists upcoming appointments for the current user.
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(PagedResult<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upcoming(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetUpcomingAppointmentsQuery(pageNumber, pageSize),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lists historical appointments for the current user.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAppointmentHistoryQuery(pageNumber, pageSize),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Returns appointments for a calendar date range.
    /// </summary>
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calendar(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAppointmentCalendarQuery(fromUtc, toUtc),
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Gets an appointment by id.
    /// </summary>
    [HttpGet("{appointmentId:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAppointmentByIdQuery(appointmentId), cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>Book appointment API request.</summary>
public sealed record BookAppointmentApiRequest(
    Guid LawyerId,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Notes,
    string TimeZoneId);

/// <summary>Cancel appointment API request.</summary>
public sealed record CancelAppointmentApiRequest(string? Reason);

/// <summary>Reschedule appointment API request.</summary>
public sealed record RescheduleAppointmentApiRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    string TimeZoneId);
