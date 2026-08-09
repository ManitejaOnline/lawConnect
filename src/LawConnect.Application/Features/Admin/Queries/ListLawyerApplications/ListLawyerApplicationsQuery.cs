using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.ListLawyerApplications;

/// <summary>
/// Lists lawyer verification applications for admins.
/// </summary>
public sealed record ListLawyerApplicationsQuery(VerificationStatus? Status, int PageNumber, int PageSize)
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
