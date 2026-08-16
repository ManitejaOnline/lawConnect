using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Queries.DownloadCaseDocument;

/// <summary>
/// Handles <see cref="DownloadCaseDocumentQuery"/>.
/// </summary>
public sealed class DownloadCaseDocumentQueryHandler : IRequestHandler<DownloadCaseDocumentQuery, Result<CaseDocumentFile>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadCaseDocumentQueryHandler"/> class.
    /// </summary>
    public DownloadCaseDocumentQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc />
    public async Task<Result<CaseDocumentFile>> Handle(DownloadCaseDocumentQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);
        CaseAccessHelper.EnsureReadAccess(caseEntity, caseEntity.Lawyer.UserId, caseEntity.Customer.UserId, userId);

        var document = await _dbContext.CaseDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.CaseId == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(CaseDocument), request.DocumentId);

        var stream = await _fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        return Result.Success(new CaseDocumentFile(document.FileName, document.ContentType, stream));
    }
}
