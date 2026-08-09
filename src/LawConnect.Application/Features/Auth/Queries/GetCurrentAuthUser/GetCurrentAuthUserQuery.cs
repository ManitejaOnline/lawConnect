using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using MediatR;

namespace LawConnect.Application.Features.Auth.Queries.GetCurrentAuthUser;

/// <summary>
/// Returns the authenticated user's auth profile.
/// </summary>
public sealed record GetCurrentAuthUserQuery : IRequest<Result<CurrentAuthUserResponse>>;
