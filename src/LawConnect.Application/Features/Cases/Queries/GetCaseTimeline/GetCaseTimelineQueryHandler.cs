using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Queries.GetCaseTimeline;

/// <summary>
/// Handles <see cref="GetCaseTimelineQuery"/>.
/// </summary>
public sealed class GetCaseTimelineQueryHandler : IRequestHandler<GetCaseTimelineQuery, Result<IReadOnlyList<CaseTimelineEntryResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCaseTimelineQueryHandler"/> class.
    /// </summary>
    public GetCaseTimelineQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CaseTimelineEntryResponse>>> Handle(GetCaseTimelineQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);
        CaseAccessHelper.EnsureReadAccess(caseEntity, caseEntity.Lawyer.UserId, caseEntity.Customer.UserId, userId);

        var updates = await _dbContext.CaseUpdates.AsNoTracking().Where(u => u.CaseId == request.CaseId).ToListAsync(cancellationToken);
        var hearings = await _dbContext.Hearings.AsNoTracking().Where(h => h.CaseId == request.CaseId).ToListAsync(cancellationToken);
        var milestones = await _dbContext.CaseMilestones.AsNoTracking().Where(m => m.CaseId == request.CaseId).ToListAsync(cancellationToken);

        var entries = new List<CaseTimelineEntryResponse>
        {
            new("CaseCreated", caseEntity.Id, caseEntity.CreatedAt, "Case opened", $"Case created: {caseEntity.Title}", caseEntity.Status.ToString())
        };

        entries.AddRange(updates.Select(u => new CaseTimelineEntryResponse("Update", u.Id, u.CreatedAt, u.Title, u.Description, u.UpdateType.ToString())));
        entries.AddRange(hearings.Select(h => new CaseTimelineEntryResponse("Hearing", h.Id, h.CreatedAt, h.HearingType ?? "Hearing", h.Purpose ?? h.Outcome, h.Status.ToString())));
        entries.AddRange(milestones.Select(m => new CaseTimelineEntryResponse("Milestone", m.Id, m.CreatedAt, m.Title, m.Description, m.Status.ToString())));

        return Result.Success<IReadOnlyList<CaseTimelineEntryResponse>>(entries.OrderByDescending(e => e.OccurredAtUtc).ToList());
    }
}
