using System.Collections.Concurrent;

namespace LawConnect.Application.Common.Realtime;

/// <summary>
/// Tracks SignalR connections for multi-device presence.
/// </summary>
public interface IPresenceTracker
{
    /// <summary>Registers a connection for a user.</summary>
    Task UserConnectedAsync(Guid userId, string connectionId);

    /// <summary>Removes a connection for a user.</summary>
    Task UserDisconnectedAsync(Guid userId, string connectionId);

    /// <summary>Returns whether the user has any active connections.</summary>
    bool IsOnline(Guid userId);

    /// <summary>Gets active connection ids for a user.</summary>
    IReadOnlyCollection<string> GetConnections(Guid userId);
}

/// <summary>
/// In-memory multi-device presence tracker (backed by process memory; suitable with sticky sessions / single node).
/// </summary>
public sealed class PresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connections = new();

    /// <inheritdoc />
    public Task UserConnectedAsync(Guid userId, string connectionId)
    {
        var set = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        set[connectionId] = 0;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UserDisconnectedAsync(Guid userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var set))
        {
            set.TryRemove(connectionId, out _);
            if (set.IsEmpty)
            {
                _connections.TryRemove(userId, out _);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool IsOnline(Guid userId) =>
        _connections.TryGetValue(userId, out var set) && !set.IsEmpty;

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetConnections(Guid userId) =>
        _connections.TryGetValue(userId, out var set)
            ? set.Keys.ToArray()
            : Array.Empty<string>();
}
