namespace LawConnect.Application.Features.Appointments.Models;

/// <summary>
/// Appointment response DTO.
/// </summary>
public sealed record AppointmentResponse(
    Guid Id,
    Guid CustomerId,
    Guid CustomerUserId,
    string CustomerFirstName,
    string CustomerLastName,
    Guid LawyerId,
    Guid LawyerUserId,
    string LawyerFirstName,
    string LawyerLastName,
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    string? Notes,
    string? CancellationReason,
    DateTime? CancelledAtUtc,
    decimal ConsultationFeeSnapshot,
    string TimeZoneId,
    bool? HasRating = null);

/// <summary>
/// Available slot response.
/// </summary>
public sealed record AvailableSlotResponse(
    DateTime StartUtc,
    DateTime EndUtc,
    string TimeZoneId);
