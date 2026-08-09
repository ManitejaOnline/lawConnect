using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class User : BaseEntity
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public Customer? Customer { get; set; }
    public Lawyer? Lawyer { get; set; }
    public NotificationPreference? NotificationPreference { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
