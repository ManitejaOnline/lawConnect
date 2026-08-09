using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerProfile;

/// <summary>
/// Handles <see cref="UpdateLawyerProfileCommand"/>.
/// </summary>
public sealed class UpdateLawyerProfileCommandHandler
    : IRequestHandler<UpdateLawyerProfileCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerProfileCommandHandler"/> class.
    /// </summary>
    public UpdateLawyerProfileCommandHandler(
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
        UpdateLawyerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.BarCouncilNumber))
        {
            var bar = request.BarCouncilNumber.Trim();
            var taken = await _dbContext.Lawyers.AnyAsync(
                l => l.BarCouncilNumber == bar && l.Id != lawyer.Id,
                cancellationToken);
            if (taken)
            {
                throw new ConflictException("Bar council number is already registered.");
            }

            lawyer.BarCouncilNumber = bar;
        }
        else
        {
            lawyer.BarCouncilNumber = null;
        }

        lawyer.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        lawyer.YearsOfExperience = request.YearsOfExperience;
        if (request.IsAcceptingAppointments)
        {
            LawyerOperationalRules.EnsureApproved(lawyer);
        }

        lawyer.IsAcceptingAppointments = request.IsAcceptingAppointments && LawyerOperationalRules.IsApproved(lawyer);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);

        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }
}
