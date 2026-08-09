using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerFees;

/// <summary>
/// Handles <see cref="UpdateLawyerFeesCommand"/>.
/// </summary>
public sealed class UpdateLawyerFeesCommandHandler
    : IRequestHandler<UpdateLawyerFeesCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerFeesCommandHandler"/> class.
    /// </summary>
    public UpdateLawyerFeesCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        ILawyerProfileCache cache)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerProfileResponse>> Handle(
        UpdateLawyerFeesCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        lawyer.ConsultationFee = request.ConsultationFee;
        lawyer.EmergencyFee = request.EmergencyFee;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);
        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }
}
