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
        // This endpoint is anonymous: compute live but never write, so public traffic cannot
        // contend with the lawyer's own updates on the Lawyer row.
        var lawyer = await _dbContext.Lawyers
            .AsNoTracking()
            .Where(l => l.Id == request.LawyerId)
            .Select(l => new { l.Id, l.RatingAverage, l.RatingCount })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        var snapshot = await _trustScoreService.ComputeAsync(lawyer.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        var breakdown = new TrustScoreBreakdown(
            Math.Round(snapshot.ClientRatingPoints, 1),
            Math.Round(snapshot.AppointmentReliabilityPoints, 1),
            Math.Round(snapshot.ResponsivenessPoints, 1),
            Math.Round(snapshot.CaseUpdatePoints, 1),
            Math.Round(snapshot.CaseManagementPoints, 1),
            Math.Round(snapshot.VerifiedComplaintPoints, 1));

        return Result.Success(new LawyerTrustScoreResponse(
            snapshot.Score,
            snapshot.Level,
            lawyer.RatingAverage,
            lawyer.RatingCount,
            breakdown,
            snapshot.CaseManagementNote));
    }
}
