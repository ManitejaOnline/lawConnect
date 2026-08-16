using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Queries.GetCaseById;

/// <summary>
/// Handles <see cref="GetCaseByIdQuery"/>.
/// </summary>
public sealed class GetCaseByIdQueryHandler : IRequestHandler<GetCaseByIdQuery, Result<CaseDetailResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCaseByIdQueryHandler"/> class.
    /// </summary>
    public GetCaseByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<CaseDetailResponse>> Handle(GetCaseByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var entity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.Customer).ThenInclude(c => c.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Specialization)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);

        CaseAccessHelper.EnsureReadAccess(entity, entity.Lawyer.UserId, entity.Customer.UserId, userId);

        var updates = await _dbContext.CaseUpdates.AsNoTracking().Where(u => u.CaseId == entity.Id).ToListAsync(cancellationToken);
        var hearings = await _dbContext.Hearings.AsNoTracking().Where(h => h.CaseId == entity.Id).ToListAsync(cancellationToken);
        var milestones = await _dbContext.CaseMilestones.AsNoTracking().Where(m => m.CaseId == entity.Id).ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        var upcoming = CaseActivityCalculator.ComputeUpcomingAction(hearings, milestones, now);
        var signals = CaseActivityCalculator.ComputeActivitySignals(entity, updates, hearings, milestones, entity.Lawyer.UserId, now);

        var rating = await _dbContext.LawyerRatings
            .AsNoTracking()
            .Where(r => r.CaseId == entity.Id)
            .Select(r => new CaseRatingResponse(r.Stars, r.ReviewText, r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(new CaseDetailResponse(CaseMapper.ToResponse(entity), upcoming, signals, rating));
    }
}
