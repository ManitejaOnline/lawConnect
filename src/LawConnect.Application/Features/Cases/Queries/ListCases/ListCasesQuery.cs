using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Queries.ListCases;

/// <summary>
/// Lists cases scoped to the current user — a lawyer sees their own cases, a customer sees theirs.
/// </summary>
public sealed record ListCasesQuery(CaseStatus? Status, int PageNumber, int PageSize) : IRequest<Result<PagedResult<CaseListItemResponse>>>;

/// <summary>
/// Validates <see cref="ListCasesQuery"/>.
/// </summary>
public sealed class ListCasesQueryValidator : AbstractValidator<ListCasesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListCasesQueryValidator"/> class.
    /// </summary>
    public ListCasesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
