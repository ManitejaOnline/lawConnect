using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

public sealed class Device : BaseEntity
{
    public Guid UserId { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string? FcmToken { get; set; }
    public DevicePlatform Platform { get; set; } = DevicePlatform.Unknown;
    public string? AppVersion { get; set; }
    public string? OsVersion { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAtUtc { get; set; }

    public User User { get; set; } = null!;
}
