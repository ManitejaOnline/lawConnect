using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Lawyers.Services;

/// <summary>
/// Recalculates a lawyer's Trust Score from real platform activity. Callers never set the score
/// directly — this is the only place <see cref="Lawyer.TrustScore"/>/<see cref="Lawyer.TrustLevel"/>
/// are written.
/// </summary>
public interface ITrustScoreService
{
    /// <summary>Recalculates and persists one lawyer's Trust Score.</summary>
    Task RecalculateAsync(Guid lawyerId, CancellationToken cancellationToken = default);

    /// <summary>Recalculates every approved lawyer — used by the periodic background sweep.</summary>
    Task RecalculateAllApprovedAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ITrustScoreService" />
public sealed class TrustScoreService : ITrustScoreService
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromDays(30);
    private static readonly TimeSpan LateCancellationThreshold = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly LawyerVerificationOptions _verification;
    private readonly ILogger<TrustScoreService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrustScoreService"/> class.
    /// </summary>
    public TrustScoreService(
        IApplicationDbContext dbContext,
        IDateTimeProvider clock,
        IOptions<LawyerVerificationOptions> verification,
        ILogger<TrustScoreService> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _verification = verification.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecalculateAllApprovedAsync(CancellationToken cancellationToken = default)
    {
        var lawyerIds = await _dbContext.Lawyers
            .Where(l => l.VerificationStatus == VerificationStatus.Approved)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        foreach (var lawyerId in lawyerIds)
        {
            await RecalculateAsync(lawyerId, cancellationToken);
        }

        _logger.LogInformation("Trust Score sweep recalculated {Count} approved lawyers", lawyerIds.Count);
    }

    /// <inheritdoc />
    public async Task RecalculateAsync(Guid lawyerId, CancellationToken cancellationToken = default)
    {
        var lawyer = await _dbContext.Lawyers.FirstOrDefaultAsync(l => l.Id == lawyerId, cancellationToken);
        if (lawyer is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var recentCutoff = now - RecentWindow;

        var clientRating = await ComputeClientRatingAsync(lawyerId, lawyer, recentCutoff, cancellationToken);
        var reliability = await ComputeAppointmentReliabilityAsync(lawyerId, lawyer.UserId, now, recentCutoff, cancellationToken);
        var responsiveness = await ComputeResponsivenessAsync(lawyerId, lawyer.UserId, recentCutoff, cancellationToken);
        var (caseUpdates, caseManagementProxy) = await ComputeCaseComponentsAsync(lawyerId, lawyer.UserId, now, recentCutoff, cancellationToken);
        var complaints = await ComputeVerifiedComplaintPointsAsync(lawyerId, cancellationToken);
        var (stallingDeduction, caseManagementNote) = await ComputeCaseStallingDeductionAsync(lawyerId, lawyer.UserId, now, cancellationToken);
        var caseManagement = Math.Max(0, caseManagementProxy - stallingDeduction);

        var completedAppointmentCount = await _dbContext.Appointments
            .CountAsync(a => a.LawyerId == lawyerId && a.Status == AppointmentStatus.Completed, cancellationToken);
        var caseCount = await _dbContext.Cases.CountAsync(c => c.LawyerId == lawyerId, cancellationToken);
        var insufficientHistory = TrustScoreCalculator.HasInsufficientHistory(
            lawyer.RatingCount,
            completedAppointmentCount,
            caseCount);

        int score;
        string level;
        if (insufficientHistory)
        {
            clientRating = 0;
            reliability = 0;
            responsiveness = 0;
            caseUpdates = 0;
            caseManagement = 0;
            complaints = 0;
            caseManagementNote = null;
            score = 0;
            level = TrustScoreCalculator.NewLawyerLevel;
        }
        else
        {
            score = TrustScoreCalculator.TotalScore(clientRating, reliability, responsiveness, caseUpdates, caseManagement, complaints);
            level = TrustScoreCalculator.TrustLevel(score);
        }

        lawyer.TrustScore = score;
        lawyer.TrustLevel = level;
        lawyer.TrustScoreUpdatedAtUtc = now;

        _dbContext.LawyerTrustScoreHistories.Add(new LawyerTrustScoreHistory
        {
            LawyerId = lawyerId,
            Score = score,
            Level = level,
            ClientRatingPoints = clientRating,
            AppointmentReliabilityPoints = reliability,
            ResponsivenessPoints = responsiveness,
            CaseUpdatePoints = caseUpdates,
            CaseManagementPoints = caseManagement,
            VerifiedComplaintPoints = complaints,
            CaseManagementNote = caseManagementNote,
            ComputedAtUtc = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recalculated Trust Score for lawyer {LawyerId}: score={Score} level={Level}",
            lawyerId,
            score,
            level);
    }

    private async Task<double> ComputeClientRatingAsync(
        Guid lawyerId,
        Lawyer lawyer,
        DateTime recentCutoff,
        CancellationToken cancellationToken)
    {
        var historicalPoints = TrustScoreCalculator.ClientRatingPoints(lawyer.RatingAverage, lawyer.RatingCount);

        var recentStars = await _dbContext.LawyerRatings
            .Where(r => r.LawyerId == lawyerId && r.CreatedAt >= recentCutoff)
            .Select(r => r.Stars)
            .ToListAsync(cancellationToken);

        var recentPoints = recentStars.Count == 0
            ? historicalPoints
            : TrustScoreCalculator.ClientRatingPoints(recentStars.Average(), recentStars.Count);

        return TrustScoreCalculator.BlendHistoricalAndRecent(historicalPoints, recentPoints);
    }

    private sealed record OccurredAppointment(
        AppointmentStatus Status,
        DateTime StartUtc,
        DateTime EndUtc,
        DateTime? CancelledAtUtc,
        Guid? CancelledByUserId);

    private async Task<double> ComputeAppointmentReliabilityAsync(
        Guid lawyerId,
        Guid lawyerUserId,
        DateTime now,
        DateTime recentCutoff,
        CancellationToken cancellationToken)
    {
        var occurred = await _dbContext.Appointments
            .Where(a => a.LawyerId == lawyerId && a.EndUtc <= now)
            .Select(a => new OccurredAppointment(a.Status, a.StartUtc, a.EndUtc, a.CancelledAtUtc, a.CancelledByUserId))
            .ToListAsync(cancellationToken);

        double Score(IReadOnlyCollection<OccurredAppointment> set)
        {
            var completed = set.Count(a => a.Status == AppointmentStatus.Completed);
            var lawyerLateCancellations = set.Count(a =>
                a.Status == AppointmentStatus.Cancelled
                && a.CancelledByUserId == lawyerUserId
                && a.CancelledAtUtc.HasValue
                && a.StartUtc - a.CancelledAtUtc.Value < LateCancellationThreshold);
            var noShows = set.Count(a => a.Status == AppointmentStatus.NoShow);
            return TrustScoreCalculator.AppointmentReliabilityPoints(completed, lawyerLateCancellations, noShows);
        }

        var historicalPoints = Score(occurred);
        var recentSet = occurred.Where(a => a.EndUtc >= recentCutoff).ToList();
        var recentPoints = recentSet.Count == 0 ? historicalPoints : Score(recentSet);

        return TrustScoreCalculator.BlendHistoricalAndRecent(historicalPoints, recentPoints);
    }

    private sealed record MessageInfo(Guid ConversationId, Guid SenderUserId, DateTime SentAtUtc, MessageType Type);

    /// <summary>Conversation ids the lawyer shares with any of the given customer user ids (1:1 conversations).</summary>
    private async Task<List<Guid>> GetSharedConversationIdsAsync(
        Guid lawyerUserId,
        IReadOnlyCollection<Guid> customerUserIds,
        CancellationToken cancellationToken)
    {
        if (customerUserIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.ConversationParticipants
            .Where(p => p.UserId == lawyerUserId)
            .Select(p => p.ConversationId)
            .Where(cid => _dbContext.ConversationParticipants
                .Any(p2 => p2.ConversationId == cid && customerUserIds.Contains(p2.UserId)))
            .ToListAsync(cancellationToken);
    }

    private async Task<double> ComputeResponsivenessAsync(
        Guid lawyerId,
        Guid lawyerUserId,
        DateTime recentCutoff,
        CancellationToken cancellationToken)
    {
        var customerUserIds = await _dbContext.Appointments
            .Where(a => a.LawyerId == lawyerId)
            .Select(a => a.Customer.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var conversationIds = await GetSharedConversationIdsAsync(lawyerUserId, customerUserIds, cancellationToken);
        if (conversationIds.Count == 0)
        {
            return TrustScoreCalculator.ResponsivenessPoints([]);
        }

        var messages = await _dbContext.Messages
            .Where(m => conversationIds.Contains(m.ConversationId))
            .Select(m => new MessageInfo(m.ConversationId, m.SenderUserId, m.SentAtUtc, m.Type))
            .ToListAsync(cancellationToken);

        var historicalWeights = new List<double>();
        var recentWeights = new List<double>();

        foreach (var group in messages.GroupBy(m => m.ConversationId))
        {
            var ordered = group.OrderBy(m => m.SentAtUtc).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].SenderUserId == lawyerUserId)
                {
                    continue;
                }

                var next = ordered.Skip(i + 1).FirstOrDefault(m => m.SenderUserId == lawyerUserId);
                if (next is null)
                {
                    continue;
                }

                var hours = BusinessHoursBetween(ordered[i].SentAtUtc, next.SentAtUtc);
                var weight = TrustScoreCalculator.ResponseTimeWeight(hours);
                historicalWeights.Add(weight);
                if (ordered[i].SentAtUtc >= recentCutoff)
                {
                    recentWeights.Add(weight);
                }
            }
        }

        var historicalPoints = TrustScoreCalculator.ResponsivenessPoints(historicalWeights);
        var recentPoints = recentWeights.Count == 0 ? historicalPoints : TrustScoreCalculator.ResponsivenessPoints(recentWeights);

        return TrustScoreCalculator.BlendHistoricalAndRecent(historicalPoints, recentPoints);
    }

    private sealed record CaseAppointment(
        Guid CustomerUserId,
        AppointmentStatus Status,
        DateTime EndUtc,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? Notes);

    private async Task<(double CaseUpdates, double CaseManagement)> ComputeCaseComponentsAsync(
        Guid lawyerId,
        Guid lawyerUserId,
        DateTime now,
        DateTime recentCutoff,
        CancellationToken cancellationToken)
    {
        var appointments = await _dbContext.Appointments
            .Where(a => a.LawyerId == lawyerId)
            .Select(a => new CaseAppointment(a.Customer.UserId, a.Status, a.EndUtc, a.CreatedAt, a.UpdatedAt, a.Notes))
            .ToListAsync(cancellationToken);

        var requiredDocumentTypes = ParseRequiredDocumentTypes();
        var uploadedTypes = await _dbContext.LawyerDocuments
            .Where(d => d.LawyerId == lawyerId)
            .Select(d => d.DocumentType)
            .Distinct()
            .ToListAsync(cancellationToken);
        var hasRequiredDocuments = requiredDocumentTypes.Count == 0
            || requiredDocumentTypes.All(uploadedTypes.Contains);

        var customerUserIds = appointments.Select(a => a.CustomerUserId).Distinct().ToList();
        var conversationIds = await GetSharedConversationIdsAsync(lawyerUserId, customerUserIds, cancellationToken);

        var conversationMessages = conversationIds.Count == 0
            ? []
            : await _dbContext.Messages
                .Where(m => conversationIds.Contains(m.ConversationId))
                .Select(m => new MessageInfo(m.ConversationId, m.SenderUserId, m.SentAtUtc, m.Type))
                .ToListAsync(cancellationToken);

        var participants = conversationIds.Count == 0
            ? []
            : await _dbContext.ConversationParticipants
                .Where(p => conversationIds.Contains(p.ConversationId))
                .Select(p => new { p.ConversationId, p.UserId })
                .ToListAsync(cancellationToken);

        Guid? ConversationFor(Guid customerUserId) => participants
            .Where(p => p.UserId == customerUserId)
            .Select(p => (Guid?)p.ConversationId)
            .FirstOrDefault(cid => participants.Any(p2 => p2.ConversationId == cid && p2.UserId == lawyerUserId));

        (int CaseUpdateSignals, int CaseManagementSignals) ScoreAppointment(CaseAppointment appointment)
        {
            var conversationId = ConversationFor(appointment.CustomerUserId);
            var conversationMsgs = conversationId is null
                ? []
                : conversationMessages.Where(m => m.ConversationId == conversationId).ToList();
            var lawyerMessages = conversationMsgs.Where(m => m.SenderUserId == lawyerUserId).ToList();

            var notesUpdated = appointment.UpdatedAt.HasValue
                && appointment.UpdatedAt.Value > appointment.CreatedAt
                && !string.IsNullOrWhiteSpace(appointment.Notes);
            var documentShared = lawyerMessages.Any(m => m.Type != MessageType.Text);
            var followUp = lawyerMessages.Any(m => m.SentAtUtc > appointment.EndUtc);
            var caseUpdateSignals = (notesUpdated ? 1 : 0) + (documentShared ? 1 : 0) + (followUp ? 1 : 0);

            var resolvedPromptly = !(appointment.Status == AppointmentStatus.Scheduled && appointment.EndUtc < now);
            var sustainedCommunication = conversationMsgs.Count >= 2;
            var caseManagementSignals = (resolvedPromptly ? 1 : 0) + (hasRequiredDocuments ? 1 : 0) + (sustainedCommunication ? 1 : 0);

            return (caseUpdateSignals, caseManagementSignals);
        }

        double CaseUpdatesFor(IReadOnlyCollection<CaseAppointment> completedSet)
        {
            var signals = completedSet.Sum(a => ScoreAppointment(a).CaseUpdateSignals);
            return TrustScoreCalculator.CaseUpdatePoints(completedSet.Count, signals);
        }

        double CaseManagementFor(IReadOnlyCollection<CaseAppointment> allSet)
        {
            var signals = allSet.Sum(a => ScoreAppointment(a).CaseManagementSignals);
            return TrustScoreCalculator.CaseManagementPoints(allSet.Count, signals);
        }

        var completed = appointments.Where(a => a.Status == AppointmentStatus.Completed).ToList();
        var historicalCaseUpdates = CaseUpdatesFor(completed);
        var recentCompleted = completed.Where(a => a.EndUtc >= recentCutoff).ToList();
        var recentCaseUpdates = recentCompleted.Count == 0 ? historicalCaseUpdates : CaseUpdatesFor(recentCompleted);

        var historicalCaseManagement = CaseManagementFor(appointments);
        var recentAll = appointments.Where(a => a.CreatedAt >= recentCutoff).ToList();
        var recentCaseManagement = recentAll.Count == 0 ? historicalCaseManagement : CaseManagementFor(recentAll);

        return (
            TrustScoreCalculator.BlendHistoricalAndRecent(historicalCaseUpdates, recentCaseUpdates),
            TrustScoreCalculator.BlendHistoricalAndRecent(historicalCaseManagement, recentCaseManagement));
    }

    /// <summary>
    /// Deducts from the Case Management component when the lawyer has cases going stale — computed
    /// fresh from live case data every call, so a case that stops stalling contributes 0 the very
    /// next recalculation with no separate "already penalized" bookkeeping needed. Scoped entirely
    /// within the Case Management component's own point budget (see the caller), so it can never
    /// touch the other five components no matter how many cases are stalling.
    /// </summary>
    private async Task<(double Deduction, string? Note)> ComputeCaseStallingDeductionAsync(
        Guid lawyerId,
        Guid lawyerUserId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var cases = await _dbContext.Cases.Where(c => c.LawyerId == lawyerId).ToListAsync(cancellationToken);
        if (cases.Count == 0)
        {
            return (0, null);
        }

        var caseIds = cases.Select(c => c.Id).ToList();
        var updates = await _dbContext.CaseUpdates.Where(u => caseIds.Contains(u.CaseId)).ToListAsync(cancellationToken);
        var hearings = await _dbContext.Hearings.Where(h => caseIds.Contains(h.CaseId)).ToListAsync(cancellationToken);
        var milestones = await _dbContext.CaseMilestones.Where(m => caseIds.Contains(m.CaseId)).ToListAsync(cancellationToken);

        var statuses = cases.Select(c => CaseActivityCalculator.ComputeStallingStatus(
            c,
            updates.Where(u => u.CaseId == c.Id).ToList(),
            hearings.Where(h => h.CaseId == c.Id).ToList(),
            milestones.Where(m => m.CaseId == c.Id).ToList(),
            lawyerUserId,
            now)).ToList();

        var (stallingCount, severeCount, deduction) = CaseActivityCalculator.ComputeLawyerStallingDeduction(statuses);
        if (deduction <= 0)
        {
            return (0, null);
        }

        var reasons = new List<string>();
        if (stallingCount > 0)
        {
            reasons.Add($"{stallingCount} case{(stallingCount == 1 ? "" : "s")} had no meaningful activity for more than 14 days");
        }

        if (severeCount > 0)
        {
            reasons.Add($"{severeCount} case{(severeCount == 1 ? "" : "s")} had no meaningful activity for more than 30 days");
        }

        var note = $"Trust Score decreased by {Math.Round(deduction)} because {string.Join(" and ", reasons)}.";
        return (deduction, note);
    }

    private async Task<double> ComputeVerifiedComplaintPointsAsync(Guid lawyerId, CancellationToken cancellationToken)
    {
        var verified = await _dbContext.LawyerComplaints
            .Where(c => c.LawyerId == lawyerId && c.Status == ComplaintStatus.Verified)
            .Select(c => c.Severity)
            .ToListAsync(cancellationToken);

        var minor = verified.Count(s => s == ComplaintSeverity.Minor);
        var serious = verified.Count(s => s == ComplaintSeverity.Serious);
        return TrustScoreCalculator.VerifiedComplaintPoints(minor, serious);
    }

    private List<DocumentType> ParseRequiredDocumentTypes()
    {
        var types = new List<DocumentType>();
        foreach (var name in _verification.RequiredDocumentTypes)
        {
            if (Enum.TryParse<DocumentType>(name, ignoreCase: true, out var parsed) && parsed != DocumentType.ProfilePhoto)
            {
                types.Add(parsed);
            }
        }

        return types.Distinct().ToList();
    }

    /// <summary>Elapsed hours between two timestamps, excluding any full Sunday spans crossed.</summary>
    private static double BusinessHoursBetween(DateTime from, DateTime to)
    {
        if (to <= from)
        {
            return 0;
        }

        var totalHours = (to - from).TotalHours;
        var excludedHours = 0.0;
        var cursor = from.Date;
        while (cursor < to.Date.AddDays(1))
        {
            if (cursor.DayOfWeek == DayOfWeek.Sunday)
            {
                var dayStart = cursor > from ? cursor : from;
                var dayEnd = cursor.AddDays(1) < to ? cursor.AddDays(1) : to;
                if (dayEnd > dayStart)
                {
                    excludedHours += (dayEnd - dayStart).TotalHours;
                }
            }

            cursor = cursor.AddDays(1);
        }

        return Math.Max(0, totalHours - excludedHours);
    }
}
