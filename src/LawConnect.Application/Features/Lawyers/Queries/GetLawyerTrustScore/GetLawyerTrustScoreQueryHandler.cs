using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerTrustScore;

/// <summary>
/// Handles <see cref="GetLawyerTrustScoreQuery"/>.
/// </summary>
public sealed class GetLawyerTrustScoreQueryHandler
    : IRequestHandler<GetLawyerTrustScoreQuery, Result<LawyerTrustScoreResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITrustScoreService _trustScoreService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetLawyerTrustScoreQueryHandler"/> class.
    /// </summary>
    public GetLawyerTrustScoreQueryHandler(IApplicationDbContext dbContext, ITrustScoreService trustScoreService)
    {
        _dbContext = dbContext;
        _trustScoreService = trustScoreService;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerTrustScoreResponse>> Handle(
        GetLawyerTrustScoreQuery request,
        CancellationToken cancellationToken)
    {
        // Intentionally tracked (not AsNoTracking): if a recalculation runs below, EF's identity
        // map updates this same in-memory instance so we don't need a second round trip to re-read it.
        var lawyer = await _dbContext.Lawyers
            .FirstOrDefaultAsync(l => l.Id == request.LawyerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        if (lawyer.TrustScore is null)
        {
            await _trustScoreService.RecalculateAsync(lawyer.Id, cancellationToken);
        }

        var latest = await _dbContext.LawyerTrustScoreHistories
            .AsNoTracking()
            .Where(h => h.LawyerId == lawyer.Id)
            .OrderByDescending(h => h.ComputedAtUtc)
            .FirstAsync(cancellationToken);

        var breakdown = new TrustScoreBreakdown(
            Math.Round(latest.ClientRatingPoints, 1),
            Math.Round(latest.AppointmentReliabilityPoints, 1),
            Math.Round(latest.ResponsivenessPoints, 1),
            Math.Round(latest.CaseUpdatePoints, 1),
            Math.Round(latest.CaseManagementPoints, 1),
            Math.Round(latest.VerifiedComplaintPoints, 1));

        return Result.Success(new LawyerTrustScoreResponse(
            lawyer.TrustScore!.Value,
            lawyer.TrustLevel!,
            lawyer.RatingAverage,
            lawyer.RatingCount,
            breakdown,
            latest.CaseManagementNote));
    }
}
