using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Application.Features.Admin.Queries.GetLawyerApplication;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Admin.Commands.ReviewLawyerApplication;

/// <summary>
/// Handles <see cref="ReviewLawyerApplicationCommand"/>.
/// </summary>
public sealed class ReviewLawyerApplicationCommandHandler
    : IRequestHandler<ReviewLawyerApplicationCommand, Result<AdminLawyerApplicationDetail>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILawyerProfileCache _cache;
    private readonly ILawyerSearchCache _searchCache;
    private readonly ISender _sender;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewLawyerApplicationCommandHandler"/> class.
    /// </summary>
    public ReviewLawyerApplicationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ILawyerProfileCache cache,
        ILawyerSearchCache searchCache,
        ISender sender)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
        _cache = cache;
        _searchCache = searchCache;
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task<Result<AdminLawyerApplicationDetail>> Handle(
        ReviewLawyerApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _dbContext.Lawyers
            .FirstOrDefaultAsync(l => l.Id == request.LawyerId, cancellationToken)
            ?? throw new NotFoundException("Lawyer application was not found.");

        var adminId = _currentUser.GetRequiredUserId();
        var now = _clock.UtcNow;
        lawyer.ReviewedAtUtc = now;
        lawyer.ReviewedByUserId = adminId;

        switch (request.Action)
        {
            case LawyerReviewAction.Approve:
                lawyer.VerificationStatus = VerificationStatus.Approved;
                lawyer.IsVerified = true;
                lawyer.VerifiedAtUtc = now;
                lawyer.IsAcceptingAppointments = true;
                lawyer.ReviewReason = null;
                break;
            case LawyerReviewAction.Reject:
                lawyer.VerificationStatus = VerificationStatus.Rejected;
                lawyer.ReviewReason = request.Reason?.Trim();
                LawyerOperationalRules.RevokeOperationalAccess(lawyer);
                break;
            case LawyerReviewAction.RequestChanges:
                lawyer.VerificationStatus = VerificationStatus.Pending;
                lawyer.ApplicationSubmittedAtUtc = null;
                lawyer.ReviewReason = request.Reason?.Trim();
                LawyerOperationalRules.RevokeOperationalAccess(lawyer);
                break;
            default:
                throw new BusinessRuleException("Unsupported review action.", "Admin.InvalidReviewAction");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);
        await _searchCache.InvalidateAllAsync(cancellationToken);
        return await _sender.Send(new GetLawyerApplicationQuery(lawyer.Id), cancellationToken);
    }
}
