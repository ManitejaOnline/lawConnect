namespace LawConnect.Domain.Enums;

/// <summary>
/// Lifecycle status for an emergency SOS request.
/// Stored as strings — new values do not require integer remapping.
/// </summary>
public enum EmergencyStatus
{
    Searching = 0,
    Notifying = 1,
    Assigned = 2,
    /// <summary>Legacy alias of <see cref="Active"/>.</summary>
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Expired = 6,
    Failed = 7,
    Created = 8,
    Calling = 9,
    Active = 10
}

/// <summary>
/// Valid SOS transitions and open-state helpers. Server is the source of truth.
/// </summary>
public static class EmergencyLifecycle
{
    public static readonly EmergencyStatus[] Open =
    [
        EmergencyStatus.Created,
        EmergencyStatus.Searching,
        EmergencyStatus.Notifying,
        EmergencyStatus.Assigned,
        EmergencyStatus.Calling,
        EmergencyStatus.Active,
        EmergencyStatus.InProgress
    ];

    public static readonly EmergencyStatus[] AssignedLike =
    [
        EmergencyStatus.Assigned,
        EmergencyStatus.Calling,
        EmergencyStatus.Active,
        EmergencyStatus.InProgress
    ];

    public static readonly EmergencyStatus[] Terminal =
    [
        EmergencyStatus.Completed,
        EmergencyStatus.Cancelled,
        EmergencyStatus.Expired,
        EmergencyStatus.Failed
    ];

    public static bool IsOpen(EmergencyStatus status) => Open.Contains(status);

    public static bool IsAssignedLike(EmergencyStatus status) => AssignedLike.Contains(status);

    public static bool IsSearching(EmergencyStatus status) =>
        status is EmergencyStatus.Created or EmergencyStatus.Searching or EmergencyStatus.Notifying;

    public static bool IsTerminal(EmergencyStatus status) => Terminal.Contains(status);

    /// <summary>Parties may open the native phone dialer for an assigned emergency.</summary>
    public static bool CanCreateCall(EmergencyStatus status) => IsAssignedLike(status);

    public static bool CanComplete(EmergencyStatus status) => IsAssignedLike(status);

    public static bool CanCancel(EmergencyStatus status) => !IsTerminal(status);

    public static bool CanAccept(EmergencyStatus status) => IsSearching(status);

    public static bool CanMarkCalling(EmergencyStatus status) =>
        status is EmergencyStatus.Assigned or EmergencyStatus.Calling;

    public static bool CanMarkActive(EmergencyStatus status) =>
        status is EmergencyStatus.Assigned or EmergencyStatus.Calling or EmergencyStatus.Active or EmergencyStatus.InProgress;
}
