using LawConnect.Domain.Enums;

namespace LawConnect.Application.Features.Emergency.Models;

/// <summary>Emergency category list item.</summary>
public sealed record EmergencyCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid SpecializationId,
    string SpecializationName,
    int DisplayOrder);

/// <summary>SOS request response.</summary>
public sealed record EmergencyRequestResponse(
    Guid RequestId,
    Guid CustomerId,
    Guid CustomerUserId,
    Guid CategoryId,
    string CategoryName,
    EmergencyStatus Status,
    string StatusName,
    double Latitude,
    double Longitude,
    int AttemptNumber,
    double CurrentRadiusKm,
    DateTime? SearchExpiresAtUtc,
    int? RemainingSeconds,
    Guid? AssignedLawyerId,
    Guid? AcceptedAssignmentId,
    string? CancellationReason,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? AssignedAtUtc,
    DateTime? CompletedAtUtc,
    string? CityName,
    string? StateName,
    string? CountryName,
    string SearchScope,
    string? SearchScopeLabel);

/// <summary>Incoming SOS payload for lawyers.</summary>
public sealed record IncomingEmergencySosResponse(
    Guid RequestId,
    Guid AssignmentId,
    Guid CategoryId,
    string CategoryName,
    double Latitude,
    double Longitude,
    double DistanceKm,
    int Rank,
    int AttemptNumber,
    DateTime ExpiresAtUtc,
    int RemainingSeconds,
    DateTime NotifiedAtUtc,
    string? CityName);

/// <summary>Timeline entry.</summary>
public sealed record EmergencyTimelineItemResponse(
    Guid Id,
    string EventType,
    string Message,
    Guid? ActorUserId,
    string? MetadataJson,
    DateTime OccurredAtUtc);

/// <summary>History list item.</summary>
public sealed record EmergencyHistoryItemResponse(
    Guid HistoryId,
    Guid RequestId,
    Guid CategoryId,
    Guid? LawyerId,
    string FinalStatus,
    int AttemptCount,
    double? AcceptanceTimeSeconds,
    double? ResponseTimeSeconds,
    string? CancellationReason,
    DateTime ClosedAtUtc);

/// <summary>Aggregate SOS metrics.</summary>
public sealed record EmergencyMetricsResponse(
    int TotalRequests,
    int CompletedCount,
    int CancelledCount,
    int ExpiredCount,
    int FailedCount,
    double CompletionPercent,
    double AcceptancePercent,
    double? AverageResponseSeconds,
    double? AverageAcceptanceSeconds);
