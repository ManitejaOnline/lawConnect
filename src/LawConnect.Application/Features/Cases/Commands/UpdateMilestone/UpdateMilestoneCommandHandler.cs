using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Commands.UpdateMilestone;

/// <summary>
/// Handles <see cref="UpdateMilestoneCommand"/>.
/// </summary>
public sealed class UpdateMilestoneCommandHandler : IRequestHandler<UpdateMilestoneCommand, Result<CaseMilestoneResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMilestoneCommandHandler"/> class.
    /// </summary>
    public UpdateMilestoneCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
    }

    /// <inheritdoc />
    public async Task<Result<CaseMilestoneResponse>> Handle(UpdateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyerId, cancellationToken);

        var milestone = await _dbContext.CaseMilestones
            .FirstOrDefaultAsync(m => m.Id == request.MilestoneId && m.CaseId == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(CaseMilestone), request.MilestoneId);

        milestone.Title = request.Title.Trim();
        milestone.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        milestone.DueDate = request.DueDate;
        milestone.Status = request.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CaseMapper.ToResponse(milestone));
    }
}
