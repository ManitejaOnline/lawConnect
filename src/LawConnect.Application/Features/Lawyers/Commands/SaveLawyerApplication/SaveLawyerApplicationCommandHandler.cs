using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.SaveLawyerApplication;

/// <summary>
/// Handles <see cref="SaveLawyerApplicationCommand"/>.
/// </summary>
public sealed class SaveLawyerApplicationCommandHandler
    : IRequestHandler<SaveLawyerApplicationCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveLawyerApplicationCommandHandler"/> class.
    /// </summary>
    public SaveLawyerApplicationCommandHandler(
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
        SaveLawyerApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        if (lawyer.VerificationStatus == VerificationStatus.Suspended)
        {
            throw new ForbiddenException("Suspended lawyer accounts cannot update an application.");
        }

        if (lawyer.VerificationStatus == VerificationStatus.Approved)
        {
            throw new BusinessRuleException(
                "Approved lawyer profiles are updated from the dashboard, not the application wizard.",
                "Lawyer.AlreadyApproved");
        }

        if (!string.IsNullOrWhiteSpace(request.StateBarCouncil))
        {
            lawyer.StateBarCouncil = request.StateBarCouncil.Trim();
        }

        if (request.BarCouncilNumber is not null)
        {
            var bar = string.IsNullOrWhiteSpace(request.BarCouncilNumber) ? null : request.BarCouncilNumber.Trim();
            if (bar is not null)
            {
                var taken = await _dbContext.Lawyers.AnyAsync(
                    l => l.BarCouncilNumber == bar && l.Id != lawyer.Id,
                    cancellationToken);
                if (taken)
                {
                    throw new ConflictException("Enrollment / registration number is already registered.");
                }
            }

            lawyer.BarCouncilNumber = bar;
        }

        if (request.YearsOfExperience.HasValue)
        {
            lawyer.YearsOfExperience = request.YearsOfExperience.Value;
        }

        if (request.ConsultationFee.HasValue)
        {
            lawyer.ConsultationFee = request.ConsultationFee.Value;
        }

        if (request.CityName is not null)
        {
            lawyer.CityName = string.IsNullOrWhiteSpace(request.CityName) ? null : request.CityName.Trim();
        }

        if (request.StateName is not null)
        {
            lawyer.StateName = string.IsNullOrWhiteSpace(request.StateName) ? null : request.StateName.Trim();
        }

        if (request.CountryName is not null)
        {
            lawyer.CountryName = string.IsNullOrWhiteSpace(request.CountryName) ? null : request.CountryName.Trim();
        }

        if (request.Bio is not null)
        {
            lawyer.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        }

        if (request.SpecializationIds is { Count: > 0 })
        {
            var ids = request.SpecializationIds.Distinct().ToArray();
            var specializations = await _dbContext.Specializations
                .Where(s => ids.Contains(s.Id) && s.IsActive)
                .ToListAsync(cancellationToken);
            if (specializations.Count != ids.Length)
            {
                throw new NotFoundException("One or more specializations were not found.");
            }

            var existing = await _dbContext.LawyerSpecializations
                .Where(s => s.LawyerId == lawyer.Id)
                .ToListAsync(cancellationToken);
            _dbContext.LawyerSpecializations.RemoveRange(existing);
            for (var i = 0; i < ids.Length; i++)
            {
                _dbContext.LawyerSpecializations.Add(new LawyerSpecialization
                {
                    LawyerId = lawyer.Id,
                    SpecializationId = ids[i],
                    YearsOfExperience = lawyer.YearsOfExperience,
                    IsPrimary = i == 0
                });
            }
        }

        if (request.Languages is not null)
        {
            var existing = await _dbContext.LawyerLanguages
                .Where(l => l.LawyerId == lawyer.Id)
                .ToListAsync(cancellationToken);
            _dbContext.LawyerLanguages.RemoveRange(existing);
            var names = request.Languages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _dbContext.LawyerLanguages.Add(new LawyerLanguage
                {
                    LawyerId = lawyer.Id,
                    LanguageName = name,
                    LanguageCode = name.Length >= 2 ? name[..2].ToLowerInvariant() : name.ToLowerInvariant(),
                    Proficiency = "Professional"
                });
            }
        }

        if (request.Education is not null)
        {
            var existing = await _dbContext.LawyerEducations
                .Where(e => e.LawyerId == lawyer.Id)
                .ToListAsync(cancellationToken);
            _dbContext.LawyerEducations.RemoveRange(existing);
            if (!string.IsNullOrWhiteSpace(request.Education))
            {
                _dbContext.LawyerEducations.Add(new LawyerEducation
                {
                    LawyerId = lawyer.Id,
                    InstitutionName = request.Education.Trim(),
                    Degree = "As provided"
                });
            }
        }

        if (request.Availability is { Count: > 0 })
        {
            var existing = await _dbContext.LawyerAvailabilities
                .Where(a => a.LawyerId == lawyer.Id)
                .ToListAsync(cancellationToken);
            _dbContext.LawyerAvailabilities.RemoveRange(existing);
            foreach (var slot in request.Availability)
            {
                _dbContext.LawyerAvailabilities.Add(new LawyerAvailability
                {
                    LawyerId = lawyer.Id,
                    DayOfWeek = slot.DayOfWeek,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    IsAvailable = slot.IsAvailable,
                    TimeZoneId = string.IsNullOrWhiteSpace(slot.TimeZoneId) ? "Asia/Kolkata" : slot.TimeZoneId
                });
            }
        }

        if (lawyer.VerificationStatus == VerificationStatus.Rejected)
        {
            lawyer.VerificationStatus = VerificationStatus.Pending;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);
        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }
}
