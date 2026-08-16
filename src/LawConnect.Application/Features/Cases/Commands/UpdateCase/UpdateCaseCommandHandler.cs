using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Commands.UpdateCase;

/// <summary>
/// Handles <see cref="UpdateCaseCommand"/>.
/// </summary>
public sealed class UpdateCaseCommandHandler : IRequestHandler<UpdateCaseCommand, Result<CaseResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCaseCommandHandler"/> class.
    /// </summary>
    public UpdateCaseCommandHandler(IApplicationDbContext dbContext, ICurrentLawyerService currentLawyer)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
    }

    /// <inheritdoc />
    public async Task<Result<CaseResponse>> Handle(UpdateCaseCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var entity = await CaseAccessHelper.GetOwnedByLawyerAsync(_dbContext, request.CaseId, lawyerId, cancellationToken);

        var specializationExists = await _dbContext.Specializations.AnyAsync(s => s.Id == request.SpecializationId, cancellationToken);
        if (!specializationExists)
        {
            throw new NotFoundException(nameof(Specialization), request.SpecializationId);
        }

        entity.SpecializationId = request.SpecializationId;
        entity.Title = request.Title.Trim();
        entity.CaseNumber = string.IsNullOrWhiteSpace(request.CaseNumber) ? null : request.CaseNumber.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.CourtName = string.IsNullOrWhiteSpace(request.CourtName) ? null : request.CourtName.Trim();
        entity.Priority = request.Priority;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await _dbContext.Cases
            .Include(c => c.Customer).ThenInclude(c => c.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Specialization)
            .FirstAsync(c => c.Id == entity.Id, cancellationToken);
        return Result.Success(CaseMapper.ToResponse(saved));
    }
}
