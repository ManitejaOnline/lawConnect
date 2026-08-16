using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Queries.ListMilestones;

/// <summary>
/// Lists all milestones on a case, ordered by due date (nulls last), then creation order.
/// </summary>
public sealed record ListMilestonesQuery(Guid CaseId) : IRequest<Result<IReadOnlyList<CaseMilestoneResponse>>>;
