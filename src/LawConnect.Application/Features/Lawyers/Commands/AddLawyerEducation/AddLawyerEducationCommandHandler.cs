using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.AddLawyerEducation;

/// <summary>
/// Handles <see cref="AddLawyerEducationCommand"/>.
/// </summary>
public sealed class AddLawyerEducationCommandHandler
    : IRequestHandler<AddLawyerEducationCommand, Result<LawyerEducationResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddLawyerEducationCommandHandler"/> class.
    /// </summary>
    public AddLawyerEducationCommandHandler(
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
    public async Task<Result<LawyerEducationResponse>> Handle(
        AddLawyerEducationCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var education = new LawyerEducation
        {
            LawyerId = lawyer.Id,
            InstitutionName = request.InstitutionName.Trim(),
            Degree = request.Degree.Trim(),
            FieldOfStudy = string.IsNullOrWhiteSpace(request.FieldOfStudy) ? null : request.FieldOfStudy.Trim(),
            StartYear = request.StartYear,
            EndYear = request.EndYear,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        _dbContext.LawyerEducations.Add(education);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);
        return Result.Success(_mapper.Map<LawyerEducationResponse>(education));
    }
}
