using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Queries.ListCaseUpdates;

/// <summary>
/// Handles <see cref="ListCaseUpdatesQuery"/>.
/// </summary>
public sealed class ListCaseUpdatesQueryHandler : IRequestHandler<ListCaseUpdatesQuery, Result<IReadOnlyList<CaseUpdateResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListCaseUpdatesQueryHandler"/> class.
    /// </summary>
    public ListCaseUpdatesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CaseUpdateResponse>>> Handle(ListCaseUpdatesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);
        CaseAccessHelper.EnsureReadAccess(caseEntity, caseEntity.Lawyer.UserId, caseEntity.Customer.UserId, userId);

        var updates = await _dbContext.CaseUpdates
            .AsNoTracking()
            .Where(u => u.CaseId == request.CaseId)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CaseUpdateResponse>>(updates.Select(CaseMapper.ToResponse).ToList());
    }
}
