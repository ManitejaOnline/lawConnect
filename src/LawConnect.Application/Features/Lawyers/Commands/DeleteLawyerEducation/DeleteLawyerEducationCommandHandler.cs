using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerEducation;

/// <summary>
/// Handles <see cref="DeleteLawyerEducationCommand"/>.
/// </summary>
public sealed class DeleteLawyerEducationCommandHandler : IRequestHandler<DeleteLawyerEducationCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLawyerEducationCommandHandler"/> class.
    /// </summary>
    public DeleteLawyerEducationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        ILawyerProfileCache cache)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteLawyerEducationCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var education = await _dbContext.LawyerEducations.FirstOrDefaultAsync(
            e => e.Id == request.EducationId && e.LawyerId == lawyerId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(LawyerEducation), request.EducationId);

        _dbContext.LawyerEducations.Remove(education);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyerId, cancellationToken);
        return Result.Success();
    }
}
