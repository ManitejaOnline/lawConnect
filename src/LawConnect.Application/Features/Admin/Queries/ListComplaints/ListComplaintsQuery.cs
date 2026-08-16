using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.ListComplaints;

/// <summary>
/// Lists lawyer complaints for admin review, optionally filtered by status.
/// </summary>
public sealed record ListComplaintsQuery(ComplaintStatus? Status, int PageNumber, int PageSize)
    : IRequest<Result<PagedResult<AdminComplaintSummary>>>;

/// <summary>
/// Validates <see cref="ListComplaintsQuery"/>.
/// </summary>
public sealed class ListComplaintsQueryValidator : AbstractValidator<ListComplaintsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListComplaintsQueryValidator"/> class.
    /// </summary>
    public ListComplaintsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
