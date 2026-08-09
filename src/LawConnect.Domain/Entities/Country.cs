using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class Country : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Iso2Code { get; set; } = string.Empty;
    public string Iso3Code { get; set; } = string.Empty;
    public string? PhoneCode { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<State> States { get; set; } = new List<State>();
}
