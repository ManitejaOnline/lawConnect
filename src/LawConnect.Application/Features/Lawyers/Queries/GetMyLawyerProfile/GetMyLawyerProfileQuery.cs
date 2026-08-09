using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerProfile;

/// <summary>
/// Returns the current user's lawyer profile.
/// </summary>
public sealed record GetMyLawyerProfileQuery : IRequest<Result<LawyerProfileResponse>>;
