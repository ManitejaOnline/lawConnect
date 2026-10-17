using System.Text.Json;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Infrastructure.Notifications;

/// <summary>
/// Notification service that persists in-app history and sends/logs FCM pushes.
/// </summary>
public sealed class FirebaseNotificationService : INotificationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly FirebaseOptions _options;
    private readonly ILogger<FirebaseNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirebaseNotificationService"/> class.
    /// </summary>
    public FirebaseNotificationService(
        IApplicationDbContext dbContext,
        IOptions<FirebaseOptions> options,
        ILogger<FirebaseNotificationService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendPushAsync(
        string deviceToken,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            return Task.CompletedTask;
        }

        // Production would call FCM HTTP v1 with ServiceAccountPath. Until credentials are configured,
        // pushes are logged so environments without Firebase still function.
        // Title/body/data can carry private chat text and names, so only metadata is logged.
        _logger.LogInformation(
            "FCM push ({Enabled}) to {TokenPrefix}… bodyLength={BodyLength} dataKeys={DataKeys}",
            _options.Enabled,
            deviceToken.Length <= 8 ? deviceToken : deviceToken[..8],
            body?.Length ?? 0,
            data is null ? string.Empty : string.Join(",", data.Keys));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendInAppAsync(
        Guid userId,
        string title,
        string body,
        string type,
        string? dataJson = null,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            Channel = NotificationChannel.InApp,
            DataJson = dataJson
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Dispatches in-app + push notifications while respecting user preferences.
/// </summary>
public sealed class UserNotificationDispatcher : IUserNotificationDispatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserNotificationDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotificationDispatcher"/> class.
    /// </summary>
    public UserNotificationDispatcher(
        IApplicationDbContext dbContext,
        INotificationService notificationService,
        ILogger<UserNotificationDispatcher> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyUserAsync(
        Guid userId,
        string title,
        string body,
        string type,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var prefs = await _dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        var enableInApp = prefs?.EnableInApp ?? true;
        var enablePush = prefs?.EnablePush ?? true;

        if (enableInApp)
        {
            await _notificationService.SendInAppAsync(
                userId,
                title,
                body,
                type,
                data is null ? null : JsonSerializer.Serialize(data),
                cancellationToken);
        }

        if (!enablePush)
        {
            return;
        }

        var tokens = await _dbContext.Devices
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.IsActive && d.FcmToken != null && d.FcmToken != string.Empty)
            .Select(d => d.FcmToken!)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            try
            {
                await _notificationService.SendPushAsync(token, title, body, data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed pushing notification to user {UserId}", userId);
            }
        }
    }
}
