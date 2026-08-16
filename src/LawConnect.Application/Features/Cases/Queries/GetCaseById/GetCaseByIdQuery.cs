using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Queries.GetCaseById;

/// <summary>
/// Gets full case detail, including the computed upcoming action and activity signals.
/// </summary>
public sealed record GetCaseByIdQuery(Guid CaseId) : IRequest<Result<CaseDetailResponse>>;
