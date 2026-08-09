using LawConnect.Domain.Enums;

namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Schedules SOS attempt timeout processing.
/// </summary>
public interface IEmergencyTimeoutScheduler
{
    /// <summary>
    /// Schedules timeout handling for a specific SOS attempt.
    /// </summary>
    void ScheduleAttemptTimeout(Guid requestId, int attemptNumber, TimeSpan delay);
}

/// <summary>
/// Processes SOS attempt timeouts (expand radius / retry / expire).
/// </summary>
public interface IEmergencyTimeoutProcessor
{
    /// <summary>
    /// Handles an attempt timeout for the given request.
    /// </summary>
    Task ProcessTimeoutAsync(Guid requestId, int attemptNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fallback sweep for rounds whose Hangfire delay job was lost.
    /// </summary>
    Task SweepOverdueRoundsAsync(CancellationToken cancellationToken = default);
}
