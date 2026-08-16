using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Queries.ListMilestones;

/// <summary>
/// Handles <see cref="ListMilestonesQuery"/>.
/// </summary>
public sealed class ListMilestonesQueryHandler : IRequestHandler<ListMilestonesQuery, Result<IReadOnlyList<CaseMilestoneResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListMilestonesQueryHandler"/> class.
    /// </summary>
    public ListMilestonesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CaseMilestoneResponse>>> Handle(ListMilestonesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);
        CaseAccessHelper.EnsureReadAccess(caseEntity, caseEntity.Lawyer.UserId, caseEntity.Customer.UserId, userId);

        var milestones = await _dbContext.CaseMilestones
            .AsNoTracking()
            .Where(m => m.CaseId == request.CaseId)
            .OrderBy(m => m.DueDate == null)
            .ThenBy(m => m.DueDate)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CaseMilestoneResponse>>(milestones.Select(CaseMapper.ToResponse).ToList());
    }
}
