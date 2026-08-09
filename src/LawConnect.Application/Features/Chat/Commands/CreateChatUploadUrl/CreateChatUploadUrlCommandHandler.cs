using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Models;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Options;
using MediatR;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Chat.Commands.CreateChatUploadUrl;

/// <summary>
/// Handles <see cref="CreateChatUploadUrlCommand"/>.
/// </summary>
public sealed class CreateChatUploadUrlCommandHandler
    : IRequestHandler<CreateChatUploadUrlCommand, Result<ChatUploadUrlResponse>>
{
    private readonly IFileStorage _fileStorage;
    private readonly StorageOptions _storageOptions;
    private readonly AwsS3Options _s3Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateChatUploadUrlCommandHandler"/> class.
    /// </summary>
    public CreateChatUploadUrlCommandHandler(
        IFileStorage fileStorage,
        IOptions<StorageOptions> storageOptions,
        IOptions<AwsS3Options> s3Options)
    {
        _fileStorage = fileStorage;
        _storageOptions = storageOptions.Value;
        _s3Options = s3Options.Value;
    }

    /// <inheritdoc />
    public async Task<Result<ChatUploadUrlResponse>> Handle(
        CreateChatUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileSizeBytes > _storageOptions.MaxUploadBytes)
        {
            throw new BusinessRuleException(
                $"File exceeds maximum size of {_storageOptions.MaxUploadBytes} bytes.",
                "Storage.FileTooLarge");
        }

        var validFor = TimeSpan.FromMinutes(_s3Options.UploadUrlExpirationMinutes);
        var upload = await _fileStorage.CreatePresignedUploadAsync(
            request.FileName,
            request.ContentType,
            "chat",
            validFor,
            cancellationToken);

        return Result.Success(new ChatUploadUrlResponse(
            upload.StorageKey,
            upload.UploadUrl.IsAbsoluteUri ? upload.UploadUrl.ToString() : upload.UploadUrl.OriginalString,
            upload.ExpiresAtUtc));
    }
}
