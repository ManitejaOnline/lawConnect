namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Sends push and in-app notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a push notification to a device token.
    /// </summary>
    /// <param name="deviceToken">FCM (or equivalent) device token.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body.</param>
    /// <param name="data">Optional key/value payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPushAsync(
        string deviceToken,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an in-app notification for a user.
    /// </summary>
    /// <param name="userId">Target user identifier.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body.</param>
    /// <param name="type">Application notification type.</param>
    /// <param name="dataJson">Optional JSON payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendInAppAsync(
        Guid userId,
        string title,
        string body,
        string type,
        string? dataJson = null,
        CancellationToken cancellationToken = default);
}
