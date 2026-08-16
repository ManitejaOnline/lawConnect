using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.AdminListCases;

/// <summary>
/// Lists all cases platform-wide, for admin search/moderation. Unlike ListCasesQuery, this query is
/// not scoped to the caller — access is gated by <c>PolicyNames.AdminOnly</c> on the controller.
/// </summary>
public sealed record AdminListCasesQuery(
    CaseStatus? Status,
    Guid? LawyerId,
    Guid? CustomerId,
    bool OverdueOnly,
    bool InactiveOnly,
    bool StallingOnly,
    int PageNumber,
    int PageSize) : IRequest<Result<PagedResult<AdminCaseListItemResponse>>>;

/// <summary>
/// Validates <see cref="AdminListCasesQuery"/>.
/// </summary>
public sealed class AdminListCasesQueryValidator : AbstractValidator<AdminListCasesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminListCasesQueryValidator"/> class.
    /// </summary>
    public AdminListCasesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
