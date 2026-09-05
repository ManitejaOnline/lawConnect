using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Admin.Queries.GetAdminLawyerTrustScore;

/// <summary>
/// Handles <see cref="GetAdminLawyerTrustScoreQuery"/>.
/// </summary>
public sealed class GetAdminLawyerTrustScoreQueryHandler
    : IRequestHandler<GetAdminLawyerTrustScoreQuery, Result<AdminLawyerTrustScoreResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITrustScoreService _trustScoreService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAdminLawyerTrustScoreQueryHandler"/> class.
    /// </summary>
    public GetAdminLawyerTrustScoreQueryHandler(IApplicationDbContext dbContext, ITrustScoreService trustScoreService)
    {
        _dbContext = dbContext;
        _trustScoreService = trustScoreService;
    }

    /// <inheritdoc />
    public async Task<Result<AdminLawyerTrustScoreResponse>> Handle(
        GetAdminLawyerTrustScoreQuery request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _dbContext.Lawyers
            .FirstOrDefaultAsync(l => l.Id == request.LawyerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        await _trustScoreService.RecalculateAsync(lawyer.Id, cancellationToken);

        var historyRows = await _dbContext.LawyerTrustScoreHistories
            .AsNoTracking()
            .Where(h => h.LawyerId == lawyer.Id)
            .OrderByDescending(h => h.ComputedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var history = historyRows.Select(h => new TrustScoreHistoryEntry(
            h.Score,
            h.Level,
            new TrustScoreBreakdown(
                Math.Round(h.ClientRatingPoints, 1),
                Math.Round(h.AppointmentReliabilityPoints, 1),
                Math.Round(h.ResponsivenessPoints, 1),
                Math.Round(h.CaseUpdatePoints, 1),
                Math.Round(h.CaseManagementPoints, 1),
                Math.Round(h.VerifiedComplaintPoints, 1)),
            h.CaseManagementNote,
            h.ComputedAtUtc)).ToArray();

        var current = history[0];

        var complaints = await _dbContext.LawyerComplaints
            .AsNoTracking()
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Client).ThenInclude(c => c.User)
            .Where(c => c.LawyerId == lawyer.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new AdminComplaintSummary(
                c.Id,
                c.LawyerId,
                $"{c.Lawyer.User.FirstName} {c.Lawyer.User.LastName}".Trim(),
                c.ClientId,
                $"{c.Client.User.FirstName} {c.Client.User.LastName}".Trim(),
                c.AppointmentId,
                c.Category,
                c.Description,
                c.Status.ToString(),
                c.Severity == null ? null : c.Severity.ToString(),
                c.AdminNotes,
                c.ReviewedAtUtc,
                c.CreatedAt))
            .ToArrayAsync(cancellationToken);

        var currentResponse = new LawyerTrustScoreResponse(
            current.Score,
            current.Level,
            lawyer.RatingAverage,
            lawyer.RatingCount,
            current.Breakdown,
            current.CaseManagementNote);

        return Result.Success(new AdminLawyerTrustScoreResponse(currentResponse, history, complaints));
    }
}
