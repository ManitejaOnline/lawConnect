namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Serializes schedule changes for a single lawyer so that "check the slot is free, then save"
/// cannot interleave between concurrent requests (which would double-book the slot).
/// </summary>
public interface ILawyerScheduleGuard
{
    /// <summary>
    /// Runs <paramref name="operation"/> while holding an exclusive, database-wide lock for the lawyer.
    /// The operation's changes commit when it completes; the lock is released at commit or rollback.
    /// The operation may be re-run on transient database failures, so it must be safe to repeat.
    /// </summary>
    Task<T> RunExclusiveAsync<T>(
        Guid lawyerId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
