using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Per-user channel preferences for notifications.
/// </summary>
public sealed class NotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public bool EnablePush { get; set; } = true;
    public bool EnableSms { get; set; } = true;
    public bool EnableEmail { get; set; } = true;
    public bool EnableInApp { get; set; } = true;

    public User User { get; set; } = null!;
}
