using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerRating;

/// <summary>
/// Handles <see cref="SubmitLawyerRatingCommand"/>.
/// </summary>
public sealed class SubmitLawyerRatingCommandHandler
    : IRequestHandler<SubmitLawyerRatingCommand, Result<LawyerRatingSubmittedResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly IDateTimeProvider _clock;
    private readonly ITrustScoreService _trustScoreService;
    private readonly ILogger<SubmitLawyerRatingCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitLawyerRatingCommandHandler"/> class.
    /// </summary>
    public SubmitLawyerRatingCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentCustomerService currentCustomer,
        IDateTimeProvider clock,
        ITrustScoreService trustScoreService,
        ILogger<SubmitLawyerRatingCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentCustomer = currentCustomer;
        _clock = clock;
        _trustScoreService = trustScoreService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerRatingSubmittedResponse>> Handle(
        SubmitLawyerRatingCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _currentCustomer.GetOrCreateAsync(cancellationToken);

        var appointment = await _dbContext.Appointments
            .Include(a => a.Lawyer)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.AppointmentId);

        if (appointment.CustomerId != customer.Id)
        {
            throw new ForbiddenException("You can only rate your own appointments.");
        }

        if (appointment.LawyerId != request.LawyerId)
        {
            throw new BusinessRuleException("Appointment does not belong to this lawyer.", "Rating.LawyerMismatch");
        }

        if (appointment.Status != AppointmentStatus.Completed)
        {
            throw new BusinessRuleException(
                "Only completed appointments can be rated.",
                "Rating.AppointmentNotCompleted");
        }

        // Defense in depth: a Customer-role caller cannot structurally be the target Lawyer's own
        // account, but guard explicitly rather than relying on that alone.
        if (appointment.Lawyer.UserId == customer.UserId)
        {
            throw new ForbiddenException("Lawyers cannot rate themselves.");
        }

        var alreadyRated = await _dbContext.LawyerRatings
            .AnyAsync(r => r.AppointmentId == request.AppointmentId, cancellationToken);
        if (alreadyRated)
        {
            throw new ConflictException("This appointment has already been rated.");
        }

        var now = _clock.UtcNow;
        var rating = new LawyerRating
        {
            LawyerId = appointment.LawyerId,
            ClientId = customer.Id,
            AppointmentId = appointment.Id,
            Stars = request.Stars,
            ReviewText = string.IsNullOrWhiteSpace(request.ReviewText) ? null : request.ReviewText.Trim()
        };
        _dbContext.LawyerRatings.Add(rating);

        var existingStats = await _dbContext.LawyerRatings
            .Where(r => r.LawyerId == appointment.LawyerId)
            .Select(r => r.Stars)
            .ToListAsync(cancellationToken);
        existingStats.Add(request.Stars);

        appointment.Lawyer.RatingCount = existingStats.Count;
        appointment.Lawyer.RatingAverage = existingStats.Average();

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Customer {CustomerId} rated lawyer {LawyerId} {Stars} stars for appointment {AppointmentId}",
            customer.Id,
            appointment.LawyerId,
            request.Stars,
            appointment.Id);

        await _trustScoreService.RecalculateAsync(appointment.LawyerId, cancellationToken);

        return Result.Success(new LawyerRatingSubmittedResponse(
            rating.Id,
            rating.LawyerId,
            rating.Stars,
            rating.ReviewText,
            now));
    }
}
