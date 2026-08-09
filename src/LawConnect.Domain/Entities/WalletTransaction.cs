#pragma warning disable CS1591
using LawConnect.Domain.Common;
using LawConnect.Domain.Enums;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Immutable wallet ledger entry.
/// </summary>
public sealed class WalletTransaction : BaseEntity
{
    public Guid WalletId { get; set; }
    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal AvailableBalanceAfter { get; set; }
    public decimal HeldBalanceAfter { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }

    public Wallet Wallet { get; set; } = null!;
}
