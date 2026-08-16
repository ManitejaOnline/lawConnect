using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Cases.Commands.SubmitCaseRating;

/// <summary>
/// Handles <see cref="SubmitCaseRatingCommand"/>.
/// </summary>
public sealed class SubmitCaseRatingCommandHandler : IRequestHandler<SubmitCaseRatingCommand, Result<CaseRatingResponse>>
{
    private static readonly CaseStatus[] RatableStatuses =
    [
        CaseStatus.Won, CaseStatus.Lost, CaseStatus.Settled, CaseStatus.Closed
    ];

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly IDateTimeProvider _clock;
    private readonly ITrustScoreService _trustScoreService;
    private readonly ILogger<SubmitCaseRatingCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitCaseRatingCommandHandler"/> class.
    /// </summary>
    public SubmitCaseRatingCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentCustomerService currentCustomer,
        IDateTimeProvider clock,
        ITrustScoreService trustScoreService,
        ILogger<SubmitCaseRatingCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentCustomer = currentCustomer;
        _clock = clock;
        _trustScoreService = trustScoreService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<CaseRatingResponse>> Handle(SubmitCaseRatingCommand request, CancellationToken cancellationToken)
    {
        var customer = await _currentCustomer.GetOrCreateAsync(cancellationToken);

        var caseEntity = await _dbContext.Cases
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);

        if (caseEntity.CustomerId != customer.Id)
        {
            throw new ForbiddenException("You can only rate your own cases.");
        }

        if (!RatableStatuses.Contains(caseEntity.Status))
        {
            throw new BusinessRuleException(
                "Only a completed or closed case can be rated.",
                "CaseRating.NotConcluded");
        }

        // Defense in depth: a Customer-role caller cannot structurally be the case's own Lawyer,
        // but guard explicitly rather than relying on that alone.
        if (caseEntity.Lawyer.UserId == customer.UserId)
        {
            throw new ForbiddenException("Lawyers cannot rate their own cases.");
        }

        var alreadyRated = await _dbContext.LawyerRatings.AnyAsync(r => r.CaseId == request.CaseId, cancellationToken);
        if (alreadyRated)
        {
            throw new ConflictException("This case has already been rated.");
        }

        var now = _clock.UtcNow;
        var rating = new LawyerRating
        {
            LawyerId = caseEntity.LawyerId,
            ClientId = customer.Id,
            CaseId = caseEntity.Id,
            Stars = request.Stars,
            ReviewText = string.IsNullOrWhiteSpace(request.ReviewText) ? null : request.ReviewText.Trim()
        };
        _dbContext.LawyerRatings.Add(rating);

        var existingStats = await _dbContext.LawyerRatings
            .Where(r => r.LawyerId == caseEntity.LawyerId)
            .Select(r => r.Stars)
            .ToListAsync(cancellationToken);
        existingStats.Add(request.Stars);

        caseEntity.Lawyer.RatingCount = existingStats.Count;
        caseEntity.Lawyer.RatingAverage = existingStats.Average();

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Customer {CustomerId} rated case {CaseId} (lawyer {LawyerId}) {Stars} stars",
            customer.Id,
            caseEntity.Id,
            caseEntity.LawyerId,
            request.Stars);

        await _trustScoreService.RecalculateAsync(caseEntity.LawyerId, cancellationToken);

        return Result.Success(new CaseRatingResponse(rating.Stars, rating.ReviewText, now));
    }
}
