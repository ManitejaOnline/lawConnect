using LawConnect.Domain.Common;

namespace LawConnect.Domain.Entities;

public sealed class City : BaseEntity
{
    public Guid StateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsActive { get; set; } = true;

    public State State { get; set; } = null!;
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
