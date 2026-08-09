using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

public sealed class OtpVerification : BaseEntity
{
    public string Destination { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public Guid? UserId { get; set; }

    public User? User { get; set; }
}
