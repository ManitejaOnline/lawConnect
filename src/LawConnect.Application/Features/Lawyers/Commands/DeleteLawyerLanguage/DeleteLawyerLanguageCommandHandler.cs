using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerLanguage;

/// <summary>
/// Handles <see cref="DeleteLawyerLanguageCommand"/>.
/// </summary>
public sealed class DeleteLawyerLanguageCommandHandler : IRequestHandler<DeleteLawyerLanguageCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLawyerLanguageCommandHandler"/> class.
    /// </summary>
    public DeleteLawyerLanguageCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        ILawyerProfileCache cache)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteLawyerLanguageCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var language = await _dbContext.LawyerLanguages.FirstOrDefaultAsync(
            l => l.Id == request.LanguageId && l.LawyerId == lawyerId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(LawyerLanguage), request.LanguageId);

        _dbContext.LawyerLanguages.Remove(language);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyerId, cancellationToken);
        return Result.Success();
    }
}
