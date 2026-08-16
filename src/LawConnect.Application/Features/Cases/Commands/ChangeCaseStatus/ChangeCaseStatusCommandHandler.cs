using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Commands.ChangeCaseStatus;

/// <summary>
/// Handles <see cref="ChangeCaseStatusCommand"/>.
/// </summary>
public sealed class ChangeCaseStatusCommandHandler : IRequestHandler<ChangeCaseStatusCommand, Result<CaseResponse>>
{
    private static readonly HashSet<CaseStatus> TerminalStatuses =
    [
        CaseStatus.Won, CaseStatus.Lost, CaseStatus.Settled, CaseStatus.Closed, CaseStatus.Cancelled
    ];

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeCaseStatusCommandHandler"/> class.
    /// </summary>
    public ChangeCaseStatusCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<CaseResponse>> Handle(ChangeCaseStatusCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var entity = await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyer.Id, cancellationToken);

        var previousStatus = entity.Status;
        var now = _clock.UtcNow;

        entity.Status = request.Status;
        entity.ClosedDate = TerminalStatuses.Contains(request.Status) ? entity.ClosedDate ?? now : null;

        if (previousStatus != request.Status)
        {
            var description = $"Status changed from {previousStatus} to {request.Status}.";
            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                description += $" {request.Note.Trim()}";
            }

            _dbContext.CaseUpdates.Add(new CaseUpdate
            {
                CaseId = entity.Id,
                CreatedByUserId = lawyer.UserId,
                UpdateType = CaseUpdateType.StatusChange,
                Title = "Status changed",
                Description = description
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await _dbContext.Cases
            .Include(c => c.Customer).ThenInclude(c => c.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Specialization)
            .FirstAsync(c => c.Id == entity.Id, cancellationToken);
        return Result.Success(CaseMapper.ToResponse(saved));
    }
}
