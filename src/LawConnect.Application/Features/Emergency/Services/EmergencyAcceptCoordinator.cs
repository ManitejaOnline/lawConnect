using System.Collections.Concurrent;

namespace LawConnect.Application.Features.Emergency.Services;

/// <summary>
/// Serializes first-accept claims per SOS request to prevent double assignment.
/// </summary>
public interface IEmergencyAcceptCoordinator
{
    /// <summary>
    /// Runs an accept claim exclusively for a request.
    /// </summary>
    Task<T> RunExclusiveAsync<T>(Guid requestId, Func<Task<T>> action, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-process accept coordinator (pair with DB status checks for multi-node safety).
/// </summary>
public sealed class EmergencyAcceptCoordinator : IEmergencyAcceptCoordinator
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    /// <inheritdoc />
    public async Task<T> RunExclusiveAsync<T>(
        Guid requestId,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(requestId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            gate.Release();
        }
    }
}
