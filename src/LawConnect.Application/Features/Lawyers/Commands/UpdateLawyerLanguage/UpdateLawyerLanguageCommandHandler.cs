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

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateLawyerLanguage;

/// <summary>
/// Handles <see cref="UpdateLawyerLanguageCommand"/>.
/// </summary>
public sealed class UpdateLawyerLanguageCommandHandler
    : IRequestHandler<UpdateLawyerLanguageCommand, Result<LawyerLanguageResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLawyerLanguageCommandHandler"/> class.
    /// </summary>
    public UpdateLawyerLanguageCommandHandler(
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
    public async Task<Result<LawyerLanguageResponse>> Handle(
        UpdateLawyerLanguageCommand request,
        CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var language = await _dbContext.LawyerLanguages.FirstOrDefaultAsync(
            l => l.Id == request.LanguageId && l.LawyerId == lawyerId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(LawyerLanguage), request.LanguageId);

        language.LanguageName = request.LanguageName.Trim();
        language.Proficiency = string.IsNullOrWhiteSpace(request.Proficiency) ? null : request.Proficiency.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyerId, cancellationToken);
        return Result.Success(_mapper.Map<LawyerLanguageResponse>(language));
    }
}
