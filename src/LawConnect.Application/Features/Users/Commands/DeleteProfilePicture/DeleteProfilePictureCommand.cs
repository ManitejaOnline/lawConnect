using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.DeleteProfilePicture;

/// <summary>
/// Deletes the current user's profile picture.
/// </summary>
public sealed record DeleteProfilePictureCommand : IRequest<Result<UserProfileResponse>>;
