namespace LawConnect.Shared.Constants;

/// <summary>
/// Emergency SOS timeline event type identifiers.
/// </summary>
public static class EmergencyTimelineEventTypes
{
    public const string Created = "created";
    public const string Searching = "searching";
    public const string LawyersNotified = "lawyers_notified";
    public const string CountdownStarted = "countdown_started";
    public const string LawyerAccepted = "lawyer_accepted";
    public const string AssignmentsCancelled = "assignments_cancelled";
    public const string AttemptTimedOut = "attempt_timed_out";
    public const string RadiusExpanded = "radius_expanded";
    public const string ScopeExpanded = "scope_expanded";
    public const string RetryStarted = "retry_started";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";
    public const string Failed = "failed";
    public const string Declined = "declined";
    public const string CallingStarted = "calling_started";
    public const string CallConnected = "call_connected";
}
