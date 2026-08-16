using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerRating;

/// <summary>
/// Gets a lawyer's aggregate rating, including the Bayesian-shrunk score used by the Trust Score.
/// </summary>
public sealed record GetLawyerRatingQuery(Guid LawyerId) : IRequest<Result<LawyerRatingSummaryResponse>>;
