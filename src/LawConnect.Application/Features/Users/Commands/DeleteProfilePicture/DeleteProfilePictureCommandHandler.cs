using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Users.Commands.DeleteProfilePicture;

/// <summary>
/// Handles <see cref="DeleteProfilePictureCommand"/>.
/// </summary>
public sealed class DeleteProfilePictureCommandHandler
    : IRequestHandler<DeleteProfilePictureCommand, Result<UserProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<DeleteProfilePictureCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteProfilePictureCommandHandler"/> class.
    /// </summary>
    public DeleteProfilePictureCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IFileStorage fileStorage,
        ILogger<DeleteProfilePictureCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<UserProfileResponse>> Handle(
        DeleteProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        var user = await UserProfileHelper.GetOrCreateProfileGraphAsync(
            _dbContext,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl))
        {
            const string marker = "/files/";
            var index = user.ProfileImageUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var key = user.ProfileImageUrl[(index + marker.Length)..].TrimStart('/');
                await _fileStorage.DeleteAsync(key, cancellationToken);
            }

            user.ProfileImageUrl = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted profile picture for user {UserId}", user.Id);
        }

        return Result.Success(UserProfileHelper.ToProfileResponse(user));
    }
}
