using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Queries.ListCaseUpdates;

/// <summary>
/// Lists all updates logged on a case, newest first.
/// </summary>
public sealed record ListCaseUpdatesQuery(Guid CaseId) : IRequest<Result<IReadOnlyList<CaseUpdateResponse>>>;
