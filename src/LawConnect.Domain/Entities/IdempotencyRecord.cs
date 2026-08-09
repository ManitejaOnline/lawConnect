#pragma warning disable CS1591
using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

/// <summary>
/// Stores idempotent API responses for payment mutations.
/// </summary>
public sealed class IdempotencyRecord : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
