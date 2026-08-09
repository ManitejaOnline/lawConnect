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

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerEducation;

/// <summary>
/// Handles <see cref="UpdateLawyerEducationCommand"/>.
/// </summary>
public sealed class UpdateLawyerEducationCommandHandler
    : IRequestHandler<UpdateLawyerEducationCommand, Result<LawyerEducationResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerEducationCommandHandler"/> class.
    /// </summary>
    public UpdateLawyerEducationCommandHandler(
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
        UpdateLawyerEducationCommand request,
        CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var education = await _dbContext.LawyerEducations.FirstOrDefaultAsync(
            e => e.Id == request.EducationId && e.LawyerId == lawyerId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(LawyerEducation), request.EducationId);

        education.InstitutionName = request.InstitutionName.Trim();
        education.Degree = request.Degree.Trim();
        education.FieldOfStudy = string.IsNullOrWhiteSpace(request.FieldOfStudy) ? null : request.FieldOfStudy.Trim();
        education.StartYear = request.StartYear;
        education.EndYear = request.EndYear;
        education.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyerId, cancellationToken);
        return Result.Success(_mapper.Map<LawyerEducationResponse>(education));
    }
}
