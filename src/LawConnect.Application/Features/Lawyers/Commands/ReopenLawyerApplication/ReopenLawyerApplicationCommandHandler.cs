using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.ReopenLawyerApplication;

/// <summary>
/// Handles <see cref="ReopenLawyerApplicationCommand"/>.
/// </summary>
public sealed class ReopenLawyerApplicationCommandHandler
    : IRequestHandler<ReopenLawyerApplicationCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReopenLawyerApplicationCommandHandler"/> class.
    /// </summary>
    public ReopenLawyerApplicationCommandHandler(
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
        ReopenLawyerApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        if (lawyer.VerificationStatus is not (VerificationStatus.Rejected or VerificationStatus.Pending))
        {
            throw new BusinessRuleException(
                "Only rejected or draft applications can be reopened.",
                "Lawyer.CannotReopen");
        }

        lawyer.VerificationStatus = VerificationStatus.Pending;
        lawyer.ApplicationSubmittedAtUtc = null;
        LawyerOperationalRules.RevokeOperationalAccess(lawyer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);
        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }
}
