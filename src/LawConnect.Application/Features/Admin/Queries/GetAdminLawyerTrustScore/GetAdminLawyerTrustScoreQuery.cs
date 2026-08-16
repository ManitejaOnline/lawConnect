using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.GetAdminLawyerTrustScore;

/// <summary>
/// Admin's full Trust Score view for a lawyer: breakdown, recalculation history, and complaints
/// with full detail.
/// </summary>
public sealed record GetAdminLawyerTrustScoreQuery(Guid LawyerId) : IRequest<Result<AdminLawyerTrustScoreResponse>>;
