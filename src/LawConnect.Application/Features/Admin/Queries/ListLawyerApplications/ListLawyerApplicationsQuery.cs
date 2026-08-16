using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.ListLawyerApplications;

/// <summary>
/// Lists lawyer verification applications for admins. When <paramref name="Statuses"/> has more than
/// one value (e.g. Pending + UnderReview, both "awaiting an admin decision"), results matching any of
/// them are returned in a single page.
/// </summary>
public sealed record ListLawyerApplicationsQuery(IReadOnlyList<VerificationStatus>? Statuses, int PageNumber, int PageSize)
    : IRequest<Result<PagedResult<AdminLawyerApplicationSummary>>>;

/// <summary>
/// Validates <see cref="ListLawyerApplicationsQuery"/>.
/// </summary>
public sealed class ListLawyerApplicationsQueryValidator : AbstractValidator<ListLawyerApplicationsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListLawyerApplicationsQueryValidator"/> class.
    /// </summary>
    public ListLawyerApplicationsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
