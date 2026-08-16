using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using MediatR;

namespace LawConnect.Application.Features.Cases.Commands.CreateHearing;

/// <summary>
/// Handles <see cref="CreateHearingCommand"/>.
/// </summary>
public sealed class CreateHearingCommandHandler : IRequestHandler<CreateHearingCommand, Result<HearingResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateHearingCommandHandler"/> class.
    /// </summary>
    public CreateHearingCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
    }

    /// <inheritdoc />
    public async Task<Result<HearingResponse>> Handle(CreateHearingCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyerId, cancellationToken);

        var entity = new Hearing
        {
            CaseId = request.CaseId,
            HearingDate = request.HearingDate,
            CourtName = string.IsNullOrWhiteSpace(request.CourtName) ? null : request.CourtName.Trim(),
            HearingType = string.IsNullOrWhiteSpace(request.HearingType) ? null : request.HearingType.Trim(),
            Purpose = string.IsNullOrWhiteSpace(request.Purpose) ? null : request.Purpose.Trim(),
            Status = HearingStatus.Scheduled
        };

        _dbContext.Hearings.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CaseMapper.ToResponse(entity));
    }
}
