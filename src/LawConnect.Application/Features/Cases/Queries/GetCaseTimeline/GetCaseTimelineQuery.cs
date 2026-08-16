using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Queries.GetCaseTimeline;

/// <summary>
/// Gets the unified chronological timeline for a case — case creation, updates, hearings, and
/// milestones merged into one feed, newest first.
/// </summary>
public sealed record GetCaseTimelineQuery(Guid CaseId) : IRequest<Result<IReadOnlyList<CaseTimelineEntryResponse>>>;
