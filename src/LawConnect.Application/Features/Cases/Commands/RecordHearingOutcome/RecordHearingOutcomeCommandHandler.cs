using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LawConnect.Domain.Exceptions;

namespace LawConnect.Application.Features.Cases.Commands.RecordHearingOutcome;

/// <summary>
/// Handles <see cref="RecordHearingOutcomeCommand"/>.
/// </summary>
public sealed class RecordHearingOutcomeCommandHandler : IRequestHandler<RecordHearingOutcomeCommand, Result<HearingResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordHearingOutcomeCommandHandler"/> class.
    /// </summary>
    public RecordHearingOutcomeCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
    }

    /// <inheritdoc />
    public async Task<Result<HearingResponse>> Handle(RecordHearingOutcomeCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyerId, cancellationToken);

        var hearing = await _dbContext.Hearings
            .FirstOrDefaultAsync(h => h.Id == request.HearingId && h.CaseId == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hearing), request.HearingId);

        hearing.Status = request.Status;
        hearing.Outcome = string.IsNullOrWhiteSpace(request.Outcome) ? null : request.Outcome.Trim();
        hearing.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        hearing.NextAction = string.IsNullOrWhiteSpace(request.NextAction) ? null : request.NextAction.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CaseMapper.ToResponse(hearing));
    }
}
