using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Emergency.Services;

/// <summary>
/// Processes SOS acceptance-window timeouts and retries. Never globally expires an SOS
/// solely because a round timed out — escalation continues until accept/cancel/complete.
/// </summary>
public sealed class EmergencyTimeoutProcessor : IEmergencyTimeoutProcessor
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmergencyDispatchService _dispatch;
    private readonly IEmergencyAcceptCoordinator _acceptCoordinator;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<EmergencyTimeoutProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmergencyTimeoutProcessor"/> class.
    /// </summary>
    public EmergencyTimeoutProcessor(
        IApplicationDbContext dbContext,
        IEmergencyDispatchService dispatch,
        IEmergencyAcceptCoordinator acceptCoordinator,
        IDateTimeProvider clock,
        ILogger<EmergencyTimeoutProcessor> logger)
    {
        _dbContext = dbContext;
        _dispatch = dispatch;
        _acceptCoordinator = acceptCoordinator;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ProcessTimeoutAsync(
        Guid requestId,
        int attemptNumber,
        CancellationToken cancellationToken = default) =>
        _acceptCoordinator.RunExclusiveAsync(
            requestId,
            () => ProcessTimeoutCoreAsync(requestId, attemptNumber, cancellationToken),
            cancellationToken);

    private async Task<bool> ProcessTimeoutCoreAsync(
        Guid requestId,
        int attemptNumber,
        CancellationToken cancellationToken)
    {
        var sos = await LoadAsync(requestId, cancellationToken);
        if (sos.AttemptNumber != attemptNumber)
        {
            _logger.LogInformation(
                "Ignoring stale SOS timeout for {RequestId} attempt {Attempt} (current attempt {Current}, status {Status})",
                requestId,
                attemptNumber,
                sos.AttemptNumber,
                sos.Status);
            return false;
        }

        if (EmergencyLifecycle.IsTerminal(sos.Status) || EmergencyLifecycle.IsAssignedLike(sos.Status))
        {
            return false;
        }

        if (sos.Status is not (EmergencyStatus.Notifying or EmergencyStatus.Searching))
        {
            return false;
        }

        _dispatch.AddTimeline(
            sos,
            EmergencyTimelineEventTypes.AttemptTimedOut,
            $"Round {attemptNumber} elapsed. Expanding to the next region. Prior lawyers remain actionable.",
            metadata: new { attemptNumber });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dispatch.EscalateRoundAsync(sos, "Acceptance window elapsed.", cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task SweepOverdueRoundsAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var overdueIds = await _dbContext.EmergencyRequests
            .AsNoTracking()
            .Where(r =>
                (r.Status == EmergencyStatus.Notifying || r.Status == EmergencyStatus.Searching)
                && r.SearchExpiresAtUtc != null
                && r.SearchExpiresAtUtc <= now)
            .Select(r => new { r.Id, r.AttemptNumber })
            .ToListAsync(cancellationToken);

        foreach (var item in overdueIds)
        {
            try
            {
                await ProcessTimeoutAsync(item.Id, item.AttemptNumber, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SOS round sweep failed for {RequestId}", item.Id);
            }
        }
    }

    private async Task<EmergencyRequest> LoadAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmergencyRequests
                   .Include(r => r.Category)
                   .Include(r => r.Customer)
                   .Include(r => r.Assignments).ThenInclude(a => a.Lawyer)
                   .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
               ?? throw new NotFoundException(nameof(EmergencyRequest), requestId);
    }
}
