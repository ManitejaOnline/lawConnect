using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.AddLawyerExperience;

/// <summary>
/// Handles <see cref="AddLawyerExperienceCommand"/>.
/// </summary>
public sealed class AddLawyerExperienceCommandHandler
    : IRequestHandler<AddLawyerExperienceCommand, Result<LawyerExperienceResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddLawyerExperienceCommandHandler"/> class.
    /// </summary>
    public AddLawyerExperienceCommandHandler(
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
        AddLawyerExperienceCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var experience = new LawyerExperience
        {
            LawyerId = lawyer.Id,
            OrganizationName = request.OrganizationName.Trim(),
            Title = request.Title.Trim(),
            StartDate = request.StartDate,
            EndDate = request.IsCurrent ? null : request.EndDate,
            IsCurrent = request.IsCurrent,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };
        _dbContext.LawyerExperiences.Add(experience);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);
        return Result.Success(_mapper.Map<LawyerExperienceResponse>(experience));
    }
}
