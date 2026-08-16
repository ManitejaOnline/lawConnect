using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerTrustScore;

/// <summary>
/// Gets a lawyer's public Trust Score breakdown, computing it on demand if it has never been
/// calculated yet (e.g. a just-approved lawyer, before the first recalculation trigger has run).
/// </summary>
public sealed record GetLawyerTrustScoreQuery(Guid LawyerId) : IRequest<Result<LawyerTrustScoreResponse>>;
