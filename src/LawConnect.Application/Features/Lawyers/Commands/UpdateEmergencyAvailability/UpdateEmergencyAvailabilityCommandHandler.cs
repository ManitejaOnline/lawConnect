using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateEmergencyAvailability;

/// <summary>
/// Handles <see cref="UpdateEmergencyAvailabilityCommand"/>.
/// </summary>
public sealed class UpdateEmergencyAvailabilityCommandHandler
    : IRequestHandler<UpdateEmergencyAvailabilityCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEmergencyAvailabilityCommandHandler"/> class.
    /// </summary>
    public UpdateEmergencyAvailabilityCommandHandler(
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
        UpdateEmergencyAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        if (request.IsAvailableForEmergency)
        {
            LawyerOperationalRules.EnsureApproved(lawyer);
        }

        lawyer.IsAvailableForEmergency = request.IsAvailableForEmergency;
        if (request.EmergencyFee.HasValue)
        {
            lawyer.EmergencyFee = request.EmergencyFee;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);
        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }
}
