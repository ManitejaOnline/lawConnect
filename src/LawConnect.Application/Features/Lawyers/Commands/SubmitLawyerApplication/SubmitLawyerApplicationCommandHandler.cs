using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerApplication;

/// <summary>
/// Handles <see cref="SubmitLawyerApplicationCommand"/>.
/// </summary>
public sealed class SubmitLawyerApplicationCommandHandler
    : IRequestHandler<SubmitLawyerApplicationCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _clock;
    private readonly ILawyerProfileCache _cache;
    private readonly LawyerVerificationOptions _verification;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitLawyerApplicationCommandHandler"/> class.
    /// </summary>
    public SubmitLawyerApplicationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IDateTimeProvider clock,
        ILawyerProfileCache cache,
        IOptions<LawyerVerificationOptions> verification)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _clock = clock;
        _cache = cache;
        _verification = verification.Value;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerProfileResponse>> Handle(
        SubmitLawyerApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        if (lawyer.VerificationStatus == VerificationStatus.Suspended)
        {
            throw new ForbiddenException("Suspended lawyer accounts cannot submit an application.");
        }

        if (lawyer.VerificationStatus == VerificationStatus.Approved)
        {
            throw new BusinessRuleException("This lawyer is already approved.", "Lawyer.AlreadyApproved");
        }

        if (string.IsNullOrWhiteSpace(lawyer.StateBarCouncil)
            || string.IsNullOrWhiteSpace(lawyer.BarCouncilNumber)
            || lawyer.YearsOfExperience < 0
            || lawyer.ConsultationFee < 0
            || string.IsNullOrWhiteSpace(lawyer.CityName)
            || string.IsNullOrWhiteSpace(lawyer.StateName)
            || string.IsNullOrWhiteSpace(lawyer.CountryName))
        {
            throw new BusinessRuleException(
                "Complete professional and consultation profile details before submitting.",
                "Lawyer.ApplicationIncomplete");
        }

        var hasPracticeAreas = await _dbContext.LawyerSpecializations.AnyAsync(
            s => s.LawyerId == lawyer.Id,
            cancellationToken);
        if (!hasPracticeAreas)
        {
            throw new BusinessRuleException("Select at least one practice area.", "Lawyer.PracticeAreaRequired");
        }

        var required = ParseRequiredTypes();
        if (required.Count > 0)
        {
            var uploaded = await _dbContext.LawyerDocuments
                .AsNoTracking()
                .Where(d => d.LawyerId == lawyer.Id)
                .Select(d => d.DocumentType)
                .Distinct()
                .ToListAsync(cancellationToken);

            var missing = required.Where(t => !uploaded.Contains(t)).ToArray();
            if (missing.Length > 0)
            {
                throw new BusinessRuleException(
                    $"Upload required documents: {string.Join(", ", missing)}.",
                    "Lawyer.DocumentsRequired");
            }
        }

        lawyer.VerificationStatus = VerificationStatus.UnderReview;
        lawyer.ApplicationSubmittedAtUtc = _clock.UtcNow;
        lawyer.ReviewReason = null;
        LawyerOperationalRules.RevokeOperationalAccess(lawyer);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);
        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }

    private IReadOnlyList<DocumentType> ParseRequiredTypes()
    {
        var types = new List<DocumentType>();
        foreach (var name in _verification.RequiredDocumentTypes)
        {
            if (Enum.TryParse<DocumentType>(name, ignoreCase: true, out var parsed)
                && parsed != DocumentType.ProfilePhoto)
            {
                types.Add(parsed);
            }
        }

        return types.Distinct().ToArray();
    }
}
