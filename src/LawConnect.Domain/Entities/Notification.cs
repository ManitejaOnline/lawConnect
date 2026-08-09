using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public string? DataJson { get; set; }

    public User User { get; set; } = null!;
}
