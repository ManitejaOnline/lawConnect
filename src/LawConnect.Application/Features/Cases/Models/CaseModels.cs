namespace LawConnect.Application.Features.Cases.Models;

/// <summary>
/// Full case detail DTO.
/// </summary>
public sealed record CaseResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid LawyerId,
    string LawyerName,
    Guid SpecializationId,
    string CaseType,
    string Title,
    string? CaseNumber,
    string? Description,
    string? CourtName,
    string Status,
    string Priority,
    DateTime StartDate,
    DateTime? ClosedDate,
    Guid? SourceAppointmentId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Lightweight case DTO for list screens.
/// </summary>
public sealed record CaseListItemResponse(
    Guid Id,
    string Title,
    string? CaseNumber,
    string OtherPartyName,
    string CaseType,
    string Status,
    string Priority,
    DateTime StartDate,
    UpcomingActionResponse? UpcomingAction,
    DateTime? LastActivityAtUtc,
    int? DaysSinceLastUpdate);

/// <summary>
/// The next hearing or milestone due on a case, whichever comes first.
/// </summary>
public sealed record UpcomingActionResponse(
    string Kind,
    DateTime When,
    string Title,
    string? Location,
    string Status);

/// <summary>
/// Objective, non-accusatory activity/inactivity signals for a case. Never a "stalling" verdict on
/// its own — <see cref="StallingStatus"/> is the one exception, and even that is a plain
/// classification of measurable facts (days since meaningful lawyer activity), not an accusation.
/// </summary>
public sealed record CaseActivitySignalsResponse(
    int? DaysSinceLastUpdate,
    int? DaysSinceLastActivity,
    int OverdueMilestoneCount,
    int MissedHearingCount,
    int PendingActionsCount,
    DateTime? LastMeaningfulLawyerActivityAtUtc,
    int? DaysSinceLastMeaningfulLawyerActivity,
    string StallingStatus);

/// <summary>
/// A case rating a customer left after the case concluded — 1-5 stars, optional review.
/// </summary>
public sealed record CaseRatingResponse(int Stars, string? ReviewText, DateTime CreatedAt);

/// <summary>
/// Case detail response: the case itself plus computed upcoming-action and activity signals.
/// </summary>
public sealed record CaseDetailResponse(
    CaseResponse Case,
    UpcomingActionResponse? UpcomingAction,
    CaseActivitySignalsResponse ActivitySignals,
    CaseRatingResponse? Rating);

/// <summary>
/// A logged case update.
/// </summary>
public sealed record CaseUpdateResponse(
    Guid Id,
    Guid CaseId,
    Guid CreatedByUserId,
    string UpdateType,
    string Title,
    string Description,
    DateTime CreatedAt);

/// <summary>
/// A case hearing.
/// </summary>
public sealed record HearingResponse(
    Guid Id,
    Guid CaseId,
    DateTime HearingDate,
    string? CourtName,
    string? HearingType,
    string? Purpose,
    string Status,
    string? Outcome,
    string? Notes,
    string? NextAction,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// A case milestone.
/// </summary>
public sealed record CaseMilestoneResponse(
    Guid Id,
    Guid CaseId,
    string Title,
    string? Description,
    string Status,
    DateTime? DueDate,
    DateTime? CompletedDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// A file attached to a case (optionally scoped to an update/hearing/milestone).
/// </summary>
public sealed record CaseDocumentResponse(
    Guid Id,
    Guid CaseId,
    Guid? CaseUpdateId,
    Guid? HearingId,
    Guid? MilestoneId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Guid UploadedByUserId,
    DateTime CreatedAt);

/// <summary>
/// One entry in a case's unified chronological timeline.
/// </summary>
public sealed record CaseTimelineEntryResponse(
    string Kind,
    Guid EntityId,
    DateTime OccurredAtUtc,
    string Title,
    string? Description,
    string? Status);
