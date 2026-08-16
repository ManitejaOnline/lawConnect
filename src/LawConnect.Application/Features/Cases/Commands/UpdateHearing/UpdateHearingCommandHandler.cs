using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Commands.UpdateHearing;

/// <summary>
/// Handles <see cref="UpdateHearingCommand"/>.
/// </summary>
public sealed class UpdateHearingCommandHandler : IRequestHandler<UpdateHearingCommand, Result<HearingResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateHearingCommandHandler"/> class.
    /// </summary>
    public UpdateHearingCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
    }

    /// <inheritdoc />
    public async Task<Result<HearingResponse>> Handle(UpdateHearingCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyerId, cancellationToken);

        var hearing = await _dbContext.Hearings
            .FirstOrDefaultAsync(h => h.Id == request.HearingId && h.CaseId == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Hearing), request.HearingId);

        if (hearing.Status != HearingStatus.Scheduled)
        {
            throw new BusinessRuleException("Only a scheduled hearing can be edited. Use record-outcome once it has taken place.", "Hearing.NotScheduled");
        }

        hearing.HearingDate = request.HearingDate;
        hearing.CourtName = string.IsNullOrWhiteSpace(request.CourtName) ? null : request.CourtName.Trim();
        hearing.HearingType = string.IsNullOrWhiteSpace(request.HearingType) ? null : request.HearingType.Trim();
        hearing.Purpose = string.IsNullOrWhiteSpace(request.Purpose) ? null : request.Purpose.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CaseMapper.ToResponse(hearing));
    }
}
