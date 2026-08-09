using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class LawyerLanguage : BaseEntity
{
    public Guid LawyerId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string? Proficiency { get; set; }

    public Lawyer Lawyer { get; set; } = null!;
}
