using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerExperience;

/// <summary>
/// Handles <see cref="UpdateLawyerExperienceCommand"/>.
/// </summary>
public sealed class UpdateLawyerExperienceCommandHandler
    : IRequestHandler<UpdateLawyerExperienceCommand, Result<LawyerExperienceResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerExperienceCommandHandler"/> class.
    /// </summary>
    public UpdateLawyerExperienceCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        ILawyerProfileCache cache,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _cache = cache;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerExperienceResponse>> Handle(
        UpdateLawyerExperienceCommand request,
        CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var experience = await _dbContext.LawyerExperiences.FirstOrDefaultAsync(
            e => e.Id == request.ExperienceId && e.LawyerId == lawyerId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(LawyerExperience), request.ExperienceId);

        experience.OrganizationName = request.OrganizationName.Trim();
        experience.Title = request.Title.Trim();
        experience.StartDate = request.StartDate;
        experience.EndDate = request.IsCurrent ? null : request.EndDate;
        experience.IsCurrent = request.IsCurrent;
        experience.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyerId, cancellationToken);
        return Result.Success(_mapper.Map<LawyerExperienceResponse>(experience));
    }
}
