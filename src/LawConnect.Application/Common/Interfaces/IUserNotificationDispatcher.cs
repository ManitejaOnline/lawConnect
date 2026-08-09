namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Fan-out helper that respects notification preferences and stores history.
/// </summary>
public interface IUserNotificationDispatcher
{
    /// <summary>
    /// Sends in-app + optional push notifications for a user.
    /// </summary>
    Task NotifyUserAsync(
        Guid userId,
        string title,
        string body,
        string type,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
