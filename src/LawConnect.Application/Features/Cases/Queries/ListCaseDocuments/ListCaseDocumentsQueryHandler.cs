using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Queries.ListCaseDocuments;

/// <summary>
/// Handles <see cref="ListCaseDocumentsQuery"/>.
/// </summary>
public sealed class ListCaseDocumentsQueryHandler : IRequestHandler<ListCaseDocumentsQuery, Result<IReadOnlyList<CaseDocumentResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListCaseDocumentsQueryHandler"/> class.
    /// </summary>
    public ListCaseDocumentsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CaseDocumentResponse>>> Handle(ListCaseDocumentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);
        CaseAccessHelper.EnsureReadAccess(caseEntity, caseEntity.Lawyer.UserId, caseEntity.Customer.UserId, userId);

        var documents = await _dbContext.CaseDocuments
            .AsNoTracking()
            .Where(d => d.CaseId == request.CaseId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CaseDocumentResponse>>(documents.Select(CaseMapper.ToResponse).ToList());
    }
}
