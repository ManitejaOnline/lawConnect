using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class State : BaseEntity
{
    public Guid CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;

    public Country Country { get; set; } = null!;
    public ICollection<City> Cities { get; set; } = new List<City>();
}
