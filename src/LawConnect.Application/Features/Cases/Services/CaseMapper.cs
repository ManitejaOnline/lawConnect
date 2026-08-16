using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Cases.Services;

/// <summary>
/// Maps case aggregates to response DTOs.
/// </summary>
public static class CaseMapper
{
    /// <summary>Maps a case with Customer.User, Lawyer.User, and Specialization loaded.</summary>
    public static CaseResponse ToResponse(Case c) => new(
        c.Id,
        c.CustomerId,
        $"{c.Customer.User.FirstName} {c.Customer.User.LastName}".Trim(),
        c.LawyerId,
        $"{c.Lawyer.User.FirstName} {c.Lawyer.User.LastName}".Trim(),
        c.SpecializationId,
        c.Specialization.Name,
        c.Title,
        c.CaseNumber,
        c.Description,
        c.CourtName,
        c.Status.ToString(),
        c.Priority.ToString(),
        c.StartDate,
        c.ClosedDate,
        c.SourceAppointmentId,
        c.CreatedAt,
        c.UpdatedAt);

    /// <summary>Maps a case update.</summary>
    public static CaseUpdateResponse ToResponse(CaseUpdate u) => new(
        u.Id, u.CaseId, u.CreatedByUserId, u.UpdateType.ToString(), u.Title, u.Description, u.CreatedAt);

    /// <summary>Maps a hearing.</summary>
    public static HearingResponse ToResponse(Hearing h) => new(
        h.Id, h.CaseId, h.HearingDate, h.CourtName, h.HearingType, h.Purpose,
        h.Status.ToString(), h.Outcome, h.Notes, h.NextAction, h.CreatedAt, h.UpdatedAt);

    /// <summary>Maps a milestone.</summary>
    public static CaseMilestoneResponse ToResponse(CaseMilestone m) => new(
        m.Id, m.CaseId, m.Title, m.Description, m.Status.ToString(), m.DueDate, m.CompletedDate, m.CreatedAt, m.UpdatedAt);

    /// <summary>Maps a case document.</summary>
    public static CaseDocumentResponse ToResponse(CaseDocument d) => new(
        d.Id, d.CaseId, d.CaseUpdateId, d.HearingId, d.MilestoneId, d.FileName, d.ContentType, d.FileSizeBytes, d.UploadedByUserId, d.CreatedAt);
}
