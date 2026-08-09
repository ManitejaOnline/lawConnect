using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Services;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Users.Commands.UploadProfilePicture;

/// <summary>
/// Handles <see cref="UploadProfilePictureCommand"/>.
/// </summary>
public sealed class UploadProfilePictureCommandHandler
    : IRequestHandler<UploadProfilePictureCommand, Result<UserProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<UploadProfilePictureCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadProfilePictureCommandHandler"/> class.
    /// </summary>
    public UploadProfilePictureCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IFileStorage fileStorage,
        IOptions<StorageOptions> storageOptions,
        ILogger<UploadProfilePictureCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<UserProfileResponse>> Handle(
        UploadProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileSizeBytes > _storageOptions.MaxUploadBytes)
        {
            throw new BusinessRuleException(
                $"File exceeds maximum size of {_storageOptions.MaxUploadBytes} bytes.",
                "Storage.FileTooLarge");
        }

        var user = await UserProfileHelper.GetOrCreateProfileGraphAsync(
            _dbContext,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        var previousUrl = user.ProfileImageUrl;
        var storageKey = await _fileStorage.UploadAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            "profiles",
            cancellationToken);

        var url = await _fileStorage.GetPresignedUrlAsync(storageKey, TimeSpan.FromDays(365), cancellationToken);
        user.ProfileImageUrl = url.IsAbsoluteUri ? url.ToString() : url.OriginalString;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousUrl))
        {
            var oldKey = ExtractStorageKey(previousUrl);
            if (oldKey is not null)
            {
                await _fileStorage.DeleteAsync(oldKey, cancellationToken);
            }
        }

        _logger.LogInformation("Updated profile picture for user {UserId}", user.Id);
        return Result.Success(UserProfileHelper.ToProfileResponse(user));
    }

    private static string? ExtractStorageKey(string url)
    {
        const string marker = "/files/";
        var index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? null : url[(index + marker.Length)..].TrimStart('/');
    }
}
