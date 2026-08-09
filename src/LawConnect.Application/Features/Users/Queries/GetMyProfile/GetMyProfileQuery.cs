using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Queries.GetMyProfile;

/// <summary>
/// Returns the authenticated user's profile.
/// </summary>
public sealed record GetMyProfileQuery : IRequest<Result<UserProfileResponse>>;
