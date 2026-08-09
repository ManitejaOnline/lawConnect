using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.SetPracticeAreas;

/// <summary>
/// Handles <see cref="SetPracticeAreasCommand"/>.
/// </summary>
public sealed class SetPracticeAreasCommandHandler
    : IRequestHandler<SetPracticeAreasCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetPracticeAreasCommandHandler"/> class.
    /// </summary>
    public SetPracticeAreasCommandHandler(
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
        SetPracticeAreasCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var ids = request.PracticeAreas.Select(p => p.SpecializationId).ToArray();
        var found = await _dbContext.Specializations
            .Where(s => ids.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (found.Count != ids.Length)
        {
            throw new NotFoundException("One or more specializations were not found.");
        }

        var existing = await _dbContext.LawyerSpecializations
            .Where(ls => ls.LawyerId == lawyer.Id)
            .ToListAsync(cancellationToken);
        _dbContext.LawyerSpecializations.RemoveRange(existing);

        foreach (var area in request.PracticeAreas)
        {
            _dbContext.LawyerSpecializations.Add(new LawyerSpecialization
            {
                LawyerId = lawyer.Id,
                SpecializationId = area.SpecializationId,
                YearsOfExperience = area.YearsOfExperience,
                IsPrimary = area.IsPrimary
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);
        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }
}
