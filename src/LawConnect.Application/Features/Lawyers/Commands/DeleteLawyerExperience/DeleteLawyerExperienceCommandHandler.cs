using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerExperience;

/// <summary>
/// Handles <see cref="DeleteLawyerExperienceCommand"/>.
/// </summary>
public sealed class DeleteLawyerExperienceCommandHandler : IRequestHandler<DeleteLawyerExperienceCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLawyerExperienceCommandHandler"/> class.
    /// </summary>
    public DeleteLawyerExperienceCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        ILawyerProfileCache cache)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteLawyerExperienceCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var experience = await _dbContext.LawyerExperiences.FirstOrDefaultAsync(
            e => e.Id == request.ExperienceId && e.LawyerId == lawyerId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(LawyerExperience), request.ExperienceId);

        _dbContext.LawyerExperiences.Remove(experience);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyerId, cancellationToken);
        return Result.Success();
    }
}
