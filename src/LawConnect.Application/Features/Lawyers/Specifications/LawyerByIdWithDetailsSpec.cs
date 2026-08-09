using LawConnect.Application.Common.Specifications;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Lawyers.Specifications;

/// <summary>
/// Loads a lawyer with related profile collections.
/// </summary>
public sealed class LawyerByIdWithDetailsSpec : SpecificationBase<Lawyer>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LawyerByIdWithDetailsSpec"/> class.
    /// </summary>
    /// <param name="lawyerId">Lawyer id.</param>
    public LawyerByIdWithDetailsSpec(Guid lawyerId)
        : base(l => l.Id == lawyerId)
    {
        AddInclude(l => l.User);
        AddInclude(l => l.LawyerSpecializations);
        AddInclude("LawyerSpecializations.Specialization");
        AddInclude(l => l.Educations);
        AddInclude(l => l.Experiences);
        AddInclude(l => l.Languages);
        AddInclude(l => l.Availabilities);
    }
}
