using LawConnect.Application.Common.Specifications;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Lawyers.Specifications;

/// <summary>
/// Finds a lawyer by owning user id.
/// </summary>
public sealed class LawyerByUserIdSpec : SpecificationBase<Lawyer>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LawyerByUserIdSpec"/> class.
    /// </summary>
    /// <param name="userId">User id.</param>
    public LawyerByUserIdSpec(Guid userId)
        : base(l => l.UserId == userId)
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
