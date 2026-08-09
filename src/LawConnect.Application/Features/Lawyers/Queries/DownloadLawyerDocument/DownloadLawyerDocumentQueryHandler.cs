using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.DownloadLawyerDocument;

/// <summary>
/// Handles <see cref="DownloadLawyerDocumentQuery"/>.
/// </summary>
public sealed class DownloadLawyerDocumentQueryHandler
    : IRequestHandler<DownloadLawyerDocumentQuery, Result<LawyerDocumentFile>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadLawyerDocumentQueryHandler"/> class.
    /// </summary>
    public DownloadLawyerDocumentQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerDocumentFile>> Handle(
        DownloadLawyerDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var document = await _dbContext.LawyerDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.LawyerId == lawyerId, cancellationToken)
            ?? throw new NotFoundException("Document was not found.");

        var stream = await _fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        return Result.Success(new LawyerDocumentFile(document.FileName, document.ContentType, stream));
    }
}
