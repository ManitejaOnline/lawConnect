using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Queries.ListHearings;

/// <summary>
/// Lists all hearings on a case, newest first.
/// </summary>
public sealed record ListHearingsQuery(Guid CaseId) : IRequest<Result<IReadOnlyList<HearingResponse>>>;
