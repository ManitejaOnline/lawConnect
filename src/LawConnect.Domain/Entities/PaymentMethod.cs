#pragma warning disable CS1591
using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Saved payment method token for a user.
/// </summary>
public sealed class PaymentMethod : BaseEntity
{
    public Guid UserId { get; set; }
    public PaymentProviderType Provider { get; set; }
    public string ProviderMethodToken { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Last4 { get; set; }
    public int? ExpMonth { get; set; }
    public int? ExpYear { get; set; }
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
}
