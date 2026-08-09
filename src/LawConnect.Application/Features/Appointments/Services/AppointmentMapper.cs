using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Appointments.Services;

/// <summary>
/// Maps appointment aggregates to response DTOs.
/// </summary>
public static class AppointmentMapper
{
    /// <summary>
    /// Maps an appointment with customer and lawyer graphs loaded.
    /// </summary>
    public static AppointmentResponse ToResponse(Appointment appointment) =>
        new(
            appointment.Id,
            appointment.CustomerId,
            appointment.Customer.UserId,
            appointment.Customer.User.FirstName,
            appointment.Customer.User.LastName,
            appointment.LawyerId,
            appointment.Lawyer.UserId,
            appointment.Lawyer.User.FirstName,
            appointment.Lawyer.User.LastName,
            appointment.StartUtc,
            appointment.EndUtc,
            appointment.Status.ToString(),
            appointment.Notes,
            appointment.CancellationReason,
            appointment.CancelledAtUtc,
            appointment.ConsultationFeeSnapshot,
            appointment.TimeZoneId);
}
