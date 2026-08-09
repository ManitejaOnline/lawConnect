using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Lawyers.Commands.UploadLawyerDocument;

/// <summary>
/// Handles <see cref="UploadLawyerDocumentCommand"/>.
/// </summary>
public sealed class UploadLawyerDocumentCommandHandler
    : IRequestHandler<UploadLawyerDocumentCommand, Result<LawyerDocumentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IFileStorage _fileStorage;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<UploadLawyerDocumentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadLawyerDocumentCommandHandler"/> class.
    /// </summary>
    public UploadLawyerDocumentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IFileStorage fileStorage,
        IOptions<StorageOptions> storageOptions,
        ILogger<UploadLawyerDocumentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _fileStorage = fileStorage;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerDocumentResponse>> Handle(
        UploadLawyerDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileSizeBytes > _storageOptions.MaxUploadBytes)
        {
            throw new BusinessRuleException(
                $"File exceeds maximum size of {_storageOptions.MaxUploadBytes} bytes.",
                "Storage.FileTooLarge");
        }

        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var storageKey = await _fileStorage.UploadAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            $"private/lawyers/{lawyer.Id:D}/documents",
            cancellationToken);

        var document = new LawyerDocument
        {
            LawyerId = lawyer.Id,
            DocumentType = request.DocumentType,
            FileName = request.FileName.Trim(),
            StorageKey = storageKey,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes
        };

        _dbContext.LawyerDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = LawyerDocumentResponse.FromEntity(
            document,
            $"/api/lawyers/me/documents/{document.Id}/file");

        _logger.LogInformation("Uploaded document {DocumentId} for lawyer {LawyerId}", document.Id, lawyer.Id);
        return Result.Success(response);
    }
}
