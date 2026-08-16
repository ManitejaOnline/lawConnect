using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerRating;

/// <summary>
/// Handles <see cref="GetLawyerRatingQuery"/>.
/// </summary>
public sealed class GetLawyerRatingQueryHandler
    : IRequestHandler<GetLawyerRatingQuery, Result<LawyerRatingSummaryResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetLawyerRatingQueryHandler"/> class.
    /// </summary>
    public GetLawyerRatingQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Result<LawyerRatingSummaryResponse>> Handle(
        GetLawyerRatingQuery request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _dbContext.Lawyers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LawyerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        var bayesianPoints = TrustScoreCalculator.ClientRatingPoints(lawyer.RatingAverage, lawyer.RatingCount);
        var bayesianScore = bayesianPoints / TrustScoreCalculator.MaxClientRating * 5.0;

        return Result.Success(new LawyerRatingSummaryResponse(lawyer.RatingAverage, lawyer.RatingCount, Math.Round(bayesianScore, 2)));
    }
}
