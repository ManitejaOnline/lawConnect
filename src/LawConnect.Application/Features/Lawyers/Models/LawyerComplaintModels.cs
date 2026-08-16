namespace LawConnect.Application.Features.Lawyers.Models;

/// <summary>
/// Confirmation returned after a complaint is submitted. Status starts Pending — a complaint has
/// zero effect on Trust Score until an admin marks it Verified.
/// </summary>
public sealed record LawyerComplaintSubmittedResponse(
    Guid ComplaintId,
    Guid LawyerId,
    string Category,
    string Status,
    DateTime CreatedAt);
