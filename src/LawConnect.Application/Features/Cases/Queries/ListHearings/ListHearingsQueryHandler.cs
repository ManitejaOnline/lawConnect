using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Queries.ListHearings;

/// <summary>
/// Handles <see cref="ListHearingsQuery"/>.
/// </summary>
public sealed class ListHearingsQueryHandler : IRequestHandler<ListHearingsQuery, Result<IReadOnlyList<HearingResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListHearingsQueryHandler"/> class.
    /// </summary>
    public ListHearingsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<HearingResponse>>> Handle(ListHearingsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);
        CaseAccessHelper.EnsureReadAccess(caseEntity, caseEntity.Lawyer.UserId, caseEntity.Customer.UserId, userId);

        var hearings = await _dbContext.Hearings
            .AsNoTracking()
            .Where(h => h.CaseId == request.CaseId)
            .OrderByDescending(h => h.HearingDate)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<HearingResponse>>(hearings.Select(CaseMapper.ToResponse).ToList());
    }
}
