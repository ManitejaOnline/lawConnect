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

namespace LawConnect.Application.Features.Lawyers.Commands.AddLawyerLanguage;

/// <summary>
/// Handles <see cref="AddLawyerLanguageCommand"/>.
/// </summary>
public sealed class AddLawyerLanguageCommandHandler
    : IRequestHandler<AddLawyerLanguageCommand, Result<LawyerLanguageResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddLawyerLanguageCommandHandler"/> class.
    /// </summary>
    public AddLawyerLanguageCommandHandler(
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
        AddLawyerLanguageCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var code = request.LanguageCode.Trim().ToLowerInvariant();
        var exists = await _dbContext.LawyerLanguages.AnyAsync(
            l => l.LawyerId == lawyer.Id && l.LanguageCode == code,
            cancellationToken);
        if (exists)
        {
            throw new ConflictException("Language already exists for this lawyer.");
        }

        var language = new LawyerLanguage
        {
            LawyerId = lawyer.Id,
            LanguageCode = code,
            LanguageName = request.LanguageName.Trim(),
            Proficiency = string.IsNullOrWhiteSpace(request.Proficiency) ? null : request.Proficiency.Trim()
        };
        _dbContext.LawyerLanguages.Add(language);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);
        return Result.Success(_mapper.Map<LawyerLanguageResponse>(language));
    }
}
