using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Queries.DownloadLawyerDocument;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Admin.Queries.DownloadLawyerDocumentForAdmin;

/// <summary>
/// Handles <see cref="DownloadLawyerDocumentForAdminQuery"/>.
/// </summary>
public sealed class DownloadLawyerDocumentForAdminQueryHandler
    : IRequestHandler<DownloadLawyerDocumentForAdminQuery, Result<LawyerDocumentFile>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadLawyerDocumentForAdminQueryHandler"/> class.
    /// </summary>
    public DownloadLawyerDocumentForAdminQueryHandler(
        IApplicationDbContext dbContext,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerDocumentFile>> Handle(
        DownloadLawyerDocumentForAdminQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _dbContext.LawyerDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == request.DocumentId && d.LawyerId == request.LawyerId,
                cancellationToken)
            ?? throw new NotFoundException("Document was not found.");

        var stream = await _fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        return Result.Success(new LawyerDocumentFile(document.FileName, document.ContentType, stream));
    }
}
