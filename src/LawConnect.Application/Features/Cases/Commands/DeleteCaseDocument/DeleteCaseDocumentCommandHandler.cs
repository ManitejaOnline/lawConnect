using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Commands.DeleteCaseDocument;

/// <summary>
/// Handles <see cref="DeleteCaseDocumentCommand"/>.
/// </summary>
public sealed class DeleteCaseDocumentCommandHandler : IRequestHandler<DeleteCaseDocumentCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCaseDocumentCommandHandler"/> class.
    /// </summary>
    public DeleteCaseDocumentCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteCaseDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var caseEntity = await _dbContext.Cases
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);

        var document = await _dbContext.CaseDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.CaseId == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(CaseDocument), request.DocumentId);

        var isUploader = document.UploadedByUserId == userId;
        var isCaseLawyer = caseEntity.Lawyer.UserId == userId;
        if (!isUploader && !isCaseLawyer)
        {
            throw new ForbiddenException("You do not have permission to delete this document.");
        }

        await _fileStorage.DeleteAsync(document.StorageKey, cancellationToken);
        _dbContext.CaseDocuments.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
