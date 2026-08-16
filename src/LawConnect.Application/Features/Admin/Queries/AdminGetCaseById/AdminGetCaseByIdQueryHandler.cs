using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Admin.Queries.AdminGetCaseById;

/// <summary>
/// Handles <see cref="AdminGetCaseByIdQuery"/>.
/// </summary>
public sealed class AdminGetCaseByIdQueryHandler : IRequestHandler<AdminGetCaseByIdQuery, Result<AdminCaseDetailResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminGetCaseByIdQueryHandler"/> class.
    /// </summary>
    public AdminGetCaseByIdQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<AdminCaseDetailResponse>> Handle(AdminGetCaseByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.Customer).ThenInclude(c => c.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Specialization)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);

        var updates = await _dbContext.CaseUpdates.AsNoTracking().Where(u => u.CaseId == entity.Id).ToListAsync(cancellationToken);
        var hearings = await _dbContext.Hearings.AsNoTracking().Where(h => h.CaseId == entity.Id).ToListAsync(cancellationToken);
        var milestones = await _dbContext.CaseMilestones.AsNoTracking().Where(m => m.CaseId == entity.Id).ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        var upcoming = CaseActivityCalculator.ComputeUpcomingAction(hearings, milestones, now);
        var signals = CaseActivityCalculator.ComputeActivitySignals(entity, updates, hearings, milestones, entity.Lawyer.UserId, now);

        var timeline = new List<CaseTimelineEntryResponse>
        {
            new("CaseCreated", entity.Id, entity.CreatedAt, "Case opened", $"Case created: {entity.Title}", entity.Status.ToString())
        };
        timeline.AddRange(updates.Select(u => new CaseTimelineEntryResponse("Update", u.Id, u.CreatedAt, u.Title, u.Description, u.UpdateType.ToString())));
        timeline.AddRange(hearings.Select(h => new CaseTimelineEntryResponse("Hearing", h.Id, h.CreatedAt, h.HearingType ?? "Hearing", h.Purpose ?? h.Outcome, h.Status.ToString())));
        timeline.AddRange(milestones.Select(m => new CaseTimelineEntryResponse("Milestone", m.Id, m.CreatedAt, m.Title, m.Description, m.Status.ToString())));

        var response = new AdminCaseDetailResponse(
            CaseMapper.ToResponse(entity),
            upcoming,
            signals,
            timeline.OrderByDescending(e => e.OccurredAtUtc).ToList(),
            hearings.OrderByDescending(h => h.HearingDate).Select(CaseMapper.ToResponse).ToList(),
            milestones.OrderBy(m => m.DueDate == null).ThenBy(m => m.DueDate).Select(CaseMapper.ToResponse).ToList(),
            updates.OrderByDescending(u => u.CreatedAt).Select(CaseMapper.ToResponse).ToList());

        return Result.Success(response);
    }
}
