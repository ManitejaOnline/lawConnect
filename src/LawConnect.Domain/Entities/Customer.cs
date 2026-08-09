using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class Customer : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PreferredLanguage { get; set; }

    public User User { get; set; } = null!;
}
