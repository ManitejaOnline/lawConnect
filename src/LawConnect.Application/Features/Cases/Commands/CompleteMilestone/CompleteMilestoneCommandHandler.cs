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

namespace LawConnect.Application.Features.Cases.Commands.CompleteMilestone;

/// <summary>
/// Handles <see cref="CompleteMilestoneCommand"/>.
/// </summary>
public sealed class CompleteMilestoneCommandHandler : IRequestHandler<CompleteMilestoneCommand, Result<CaseMilestoneResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteMilestoneCommandHandler"/> class.
    /// </summary>
    public CompleteMilestoneCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<CaseMilestoneResponse>> Handle(CompleteMilestoneCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyerId, cancellationToken);

        var milestone = await _dbContext.CaseMilestones
            .FirstOrDefaultAsync(m => m.Id == request.MilestoneId && m.CaseId == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(CaseMilestone), request.MilestoneId);

        milestone.Status = MilestoneStatus.Completed;
        milestone.CompletedDate = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CaseMapper.ToResponse(milestone));
    }
}
