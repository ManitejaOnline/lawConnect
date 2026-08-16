namespace LawConnect.Application.Features.Admin.Models;

/// <summary>
/// Complaint summary for the admin review queue.
/// </summary>
public sealed record AdminComplaintSummary(
    Guid ComplaintId,
    Guid LawyerId,
    string LawyerName,
    Guid ClientId,
    string ClientName,
    Guid? AppointmentId,
    string Category,
    string Description,
    string Status,
    string? Severity,
    string? AdminNotes,
    DateTime? ReviewedAtUtc,
    DateTime CreatedAtUtc);
