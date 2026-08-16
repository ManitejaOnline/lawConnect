using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.CreateMilestone;

/// <summary>
/// Handles <see cref="CreateMilestoneCommand"/>.
/// </summary>
public sealed class CreateMilestoneCommandHandler : IRequestHandler<CreateMilestoneCommand, Result<CaseMilestoneResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateMilestoneCommandHandler"/> class.
    /// </summary>
    public CreateMilestoneCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
    }

    /// <inheritdoc />
    public async Task<Result<CaseMilestoneResponse>> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyerId, cancellationToken);

        var entity = new CaseMilestone
        {
            CaseId = request.CaseId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = MilestoneStatus.Pending,
            DueDate = request.DueDate
        };

        _dbContext.CaseMilestones.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CaseMapper.ToResponse(entity));
    }
}
