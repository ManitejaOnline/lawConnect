using Hangfire;
using LawConnect.Application.Common.Interfaces;

namespace LawConnect.Infrastructure.Emergency;

/// <summary>
/// Hangfire-backed SOS timeout scheduler.
/// </summary>
public sealed class HangfireEmergencyTimeoutScheduler : IEmergencyTimeoutScheduler
{
    /// <inheritdoc />
    public void ScheduleAttemptTimeout(Guid requestId, int attemptNumber, TimeSpan delay)
    {
        BackgroundJob.Schedule<IEmergencyTimeoutProcessor>(
            processor => processor.ProcessTimeoutAsync(requestId, attemptNumber, CancellationToken.None),
            delay);
    }
}

/// <summary>
/// Test/dev scheduler that records timeouts without Hangfire.
/// </summary>
public sealed class InlineEmergencyTimeoutScheduler : IEmergencyTimeoutScheduler
{
    private readonly List<(Guid RequestId, int AttemptNumber, TimeSpan Delay)> _scheduled = [];

    /// <summary>Gets scheduled timeout jobs.</summary>
    public IReadOnlyList<(Guid RequestId, int AttemptNumber, TimeSpan Delay)> Scheduled => _scheduled;

    /// <inheritdoc />
    public void ScheduleAttemptTimeout(Guid requestId, int attemptNumber, TimeSpan delay) =>
        _scheduled.Add((requestId, attemptNumber, delay));
}
