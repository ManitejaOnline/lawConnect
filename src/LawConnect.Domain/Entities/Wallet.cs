#pragma warning disable CS1591
using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// User wallet with available and held balances.
/// </summary>
public sealed class Wallet : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal HeldBalance { get; set; }
    public string Currency { get; set; } = "INR";

    public User User { get; set; } = null!;
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
