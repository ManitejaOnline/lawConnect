using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.CreateCaseUpdate;

/// <summary>
/// Handles <see cref="CreateCaseUpdateCommand"/>.
/// </summary>
public sealed class CreateCaseUpdateCommandHandler : IRequestHandler<CreateCaseUpdateCommand, Result<CaseUpdateResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCaseUpdateCommandHandler"/> class.
    /// </summary>
    public CreateCaseUpdateCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
    }

    /// <inheritdoc />
    public async Task<Result<CaseUpdateResponse>> Handle(CreateCaseUpdateCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyer.Id, cancellationToken);

        var entity = new CaseUpdate
        {
            CaseId = request.CaseId,
            CreatedByUserId = lawyer.UserId,
            UpdateType = request.UpdateType,
            Title = request.Title.Trim(),
            Description = request.Description.Trim()
        };

        _dbContext.CaseUpdates.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CaseMapper.ToResponse(entity));
    }
}
