using System.Text.Json;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Features.Emergency.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Constants;
using LawConnect.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Emergency.Services;

/// <summary>
/// Orchestrates SOS search, notify, timeout retry, accept, and closure.
/// </summary>
public interface IEmergencyDispatchService
{
    /// <summary>Runs a search/notify attempt for an existing request.</summary>
    Task<EmergencyRequestResponse> DispatchAttemptAsync(
        EmergencyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the current notify round and starts the next geographic group (city → state → country).
    /// Does not globally expire the SOS. Prior assignments stay actionable.
    /// </summary>
    Task<EmergencyRequestResponse> EscalateRoundAsync(
        EmergencyRequest request,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>Maps a request entity to API response.</summary>
    EmergencyRequestResponse ToResponse(EmergencyRequest request, string? categoryName = null);

    /// <summary>Appends a timeline event.</summary>
    void AddTimeline(
        EmergencyRequest request,
        string eventType,
        string message,
        Guid? actorUserId = null,
        object? metadata = null);

    /// <summary>Writes history snapshot for a terminal request.</summary>
    Task WriteHistoryAsync(EmergencyRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default SOS dispatch orchestration.
/// </summary>
public sealed class EmergencyDispatchService : IEmergencyDispatchService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmergencyLawyerMatcher _matcher;
    private readonly IEmergencyRealtimePublisher _realtime;
    private readonly IUserNotificationDispatcher _notifications;
    private readonly IEmergencyTimeoutScheduler _timeoutScheduler;
    private readonly IDateTimeProvider _clock;
    private readonly EmergencyOptions _options;
    private readonly ILogger<EmergencyDispatchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmergencyDispatchService"/> class.
    /// </summary>
    public EmergencyDispatchService(
        IApplicationDbContext dbContext,
        IEmergencyLawyerMatcher matcher,
        IEmergencyRealtimePublisher realtime,
        IUserNotificationDispatcher notifications,
        IEmergencyTimeoutScheduler timeoutScheduler,
        IDateTimeProvider clock,
        IOptions<EmergencyOptions> options,
        ILogger<EmergencyDispatchService> logger)
    {
        _dbContext = dbContext;
        _matcher = matcher;
        _realtime = realtime;
        _notifications = notifications;
        _timeoutScheduler = timeoutScheduler;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmergencyRequestResponse> DispatchAttemptAsync(
        EmergencyRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = request.Category
                       ?? await _dbContext.EmergencyCategories
                           .AsNoTracking()
                           .FirstAsync(c => c.Id == request.CategoryId, cancellationToken);

        var previouslyNotified = await _dbContext.EmergencyAssignments
            .AsNoTracking()
            .Where(a => a.EmergencyRequestId == request.Id)
            .Select(a => a.LawyerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        request.CurrentSearchScope = (EmergencySearchScope)_options.ScopeNumberForAttempt(request.AttemptNumber);
        request.Status = EmergencyStatus.Searching;
        request.SearchStartedAtUtc = _clock.UtcNow;
        AddTimeline(
            request,
            EmergencyTimelineEventTypes.Searching,
            EmergencySearchCopy.Searching(
                request.CurrentSearchScope,
                request.CustomerCityName,
                request.CustomerStateName,
                request.CustomerCountryName),
            metadata: new { request.AttemptNumber, Scope = request.CurrentSearchScope.ToString() });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ranked = await _matcher.FindRankedAsync(
            category.SpecializationId,
            request.Latitude,
            request.Longitude,
            request.CustomerCityId,
            request.CustomerStateId,
            request.CustomerCountryId,
            request.CurrentSearchScope,
            _options.MaxLawyersPerAttempt,
            previouslyNotified,
            cancellationToken);

        if (ranked.Count == 0)
        {
            if (_options.HasNextScope(request.AttemptNumber))
            {
                var nextScope = (EmergencySearchScope)_options.ScopeNumberForAttempt(request.AttemptNumber + 1);
                return await EscalateRoundAsync(
                    request,
                    EmergencySearchCopy.NoLawyersExpanding(
                        request.CurrentSearchScope,
                        nextScope,
                        request.CustomerCityName,
                        request.CustomerStateName,
                        request.CustomerCountryName),
                    cancellationToken);
            }

            var waitUntil = _clock.UtcNow.AddSeconds(_options.AcceptanceTimeoutSeconds);
            request.Status = EmergencyStatus.Searching;
            request.SearchExpiresAtUtc = waitUntil;
            AddTimeline(
                request,
                EmergencyTimelineEventTypes.Searching,
                "Still searching for an available lawyer.",
                metadata: new { request.AttemptNumber, Scope = request.CurrentSearchScope.ToString() });
            await _dbContext.SaveChangesAsync(cancellationToken);
            _timeoutScheduler.ScheduleAttemptTimeout(
                request.Id,
                request.AttemptNumber,
                TimeSpan.FromSeconds(_options.AcceptanceTimeoutSeconds));
            var waiting = ToResponse(request, category.Name);
            await PublishCustomerAsync(request, waiting, cancellationToken);
            return waiting;
        }

        var now = _clock.UtcNow;
        var expiresAt = now.AddSeconds(_options.AcceptanceTimeoutSeconds);
        request.Status = EmergencyStatus.Notifying;
        request.SearchExpiresAtUtc = expiresAt;

        var assignments = new List<EmergencyAssignment>();
        var rank = 1;
        foreach (var lawyer in ranked)
        {
            var assignment = new EmergencyAssignment
            {
                EmergencyRequestId = request.Id,
                LawyerId = lawyer.LawyerId,
                AttemptNumber = request.AttemptNumber,
                Rank = rank++,
                DistanceKm = lawyer.DistanceKm,
                Score = lawyer.Score,
                Status = EmergencyAssignmentStatus.Pending,
                NotifiedAtUtc = now
            };
            assignments.Add(assignment);
            _dbContext.EmergencyAssignments.Add(assignment);
        }

        AddTimeline(
            request,
            EmergencyTimelineEventTypes.LawyersNotified,
            $"Notified {assignments.Count} lawyer(s).",
            metadata: new { Count = assignments.Count, request.AttemptNumber });
        AddTimeline(
            request,
            EmergencyTimelineEventTypes.CountdownStarted,
            $"Acceptance window open for {_options.AcceptanceTimeoutSeconds} seconds.",
            metadata: new { ExpiresAtUtc = expiresAt, request.AttemptNumber });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _timeoutScheduler.ScheduleAttemptTimeout(
            request.Id,
            request.AttemptNumber,
            TimeSpan.FromSeconds(_options.AcceptanceTimeoutSeconds));

        var remaining = _options.AcceptanceTimeoutSeconds;
        var response = ToResponse(request, category.Name);
        await PublishCustomerAsync(request, response, cancellationToken);
        await _realtime.PublishCountdownAsync(
            request.Id,
            request.Customer.UserId,
            request.AttemptNumber,
            expiresAt,
            remaining,
            cancellationToken);

        foreach (var assignment in assignments)
        {
            var lawyer = ranked.First(r => r.LawyerId == assignment.LawyerId);
            var incoming = new IncomingEmergencySosResponse(
                request.Id,
                assignment.Id,
                category.Id,
                category.Name,
                request.Latitude,
                request.Longitude,
                assignment.DistanceKm,
                assignment.Rank,
                request.AttemptNumber,
                expiresAt,
                remaining,
                assignment.NotifiedAtUtc,
                request.CustomerCityName);

            await _realtime.PublishIncomingSosAsync(lawyer.UserId, incoming, cancellationToken);
            await _notifications.NotifyUserAsync(
                lawyer.UserId,
                "Emergency legal request",
                $"{category.Name} · ~{assignment.DistanceKm:0.0} km away. Tap to view emergency.",
                NotificationTypes.EmergencyIncoming,
                new Dictionary<string, string>
                {
                    ["type"] = NotificationTypes.EmergencyIncoming,
                    ["title"] = "Emergency legal request",
                    ["categoryName"] = category.Name,
                    ["distanceKm"] = assignment.DistanceKm.ToString("0.0"),
                    ["screen"] = "LawyerEmergencyIncoming",
                    ["deepLink"] =
                        $"lawconnect://emergency/incoming?requestId={request.Id:D}&assignmentId={assignment.Id:D}",
                    ["requestId"] = request.Id.ToString("D"),
                    ["assignmentId"] = assignment.Id.ToString("D"),
                    ["expiresAtUtc"] = expiresAt.ToString("O"),
                    ["receivedAtUtc"] = now.ToString("O")
                },
                cancellationToken);
        }

        _logger.LogInformation(
            "SOS {RequestId} attempt {Attempt} notified {Count} lawyers in {Scope} {Label}",
            request.Id,
            request.AttemptNumber,
            assignments.Count,
            request.CurrentSearchScope,
            request.CustomerCityName ?? request.CustomerStateName ?? request.CustomerCountryName);

        return response;
    }

    /// <inheritdoc />
    public async Task<EmergencyRequestResponse> EscalateRoundAsync(
        EmergencyRequest request,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (EmergencyLifecycle.IsTerminal(request.Status) || EmergencyLifecycle.IsAssignedLike(request.Status))
        {
            return ToResponse(request, request.Category?.Name);
        }

        var nextAttempt = request.AttemptNumber + 1;
        var nextScope = (EmergencySearchScope)_options.ScopeNumberForAttempt(nextAttempt);
        request.AttemptNumber = nextAttempt;
        request.CurrentSearchScope = nextScope;
        request.CurrentRadiusKm = 0;
        request.Status = EmergencyStatus.Searching;
        request.SearchExpiresAtUtc = null;

        AddTimeline(
            request,
            EmergencyTimelineEventTypes.ScopeExpanded,
            reason,
            metadata: new { request.AttemptNumber, Scope = nextScope.ToString(), reason });
        AddTimeline(
            request,
            EmergencyTimelineEventTypes.RetryStarted,
            EmergencySearchCopy.Searching(
                nextScope,
                request.CustomerCityName,
                request.CustomerStateName,
                request.CustomerCountryName),
            metadata: new { request.AttemptNumber, Scope = nextScope.ToString(), reason });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await DispatchAttemptAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public EmergencyRequestResponse ToResponse(EmergencyRequest request, string? categoryName = null)
    {
        var remaining = request.SearchExpiresAtUtc.HasValue
            ? Math.Max(0, (int)Math.Ceiling((request.SearchExpiresAtUtc.Value - _clock.UtcNow).TotalSeconds))
            : (int?)null;

        var scope = request.CurrentSearchScope;
        return new EmergencyRequestResponse(
            request.Id,
            request.CustomerId,
            request.Customer?.UserId ?? Guid.Empty,
            request.CategoryId,
            categoryName ?? request.Category?.Name ?? string.Empty,
            request.Status,
            request.Status.ToString(),
            request.Latitude,
            request.Longitude,
            request.AttemptNumber,
            request.CurrentRadiusKm,
            request.SearchExpiresAtUtc,
            remaining,
            request.AssignedLawyerId,
            request.AcceptedAssignmentId,
            request.CancellationReason,
            request.Notes,
            request.CreatedAt,
            request.AssignedAtUtc,
            request.CompletedAtUtc,
            request.CustomerCityName,
            request.CustomerStateName,
            request.CustomerCountryName,
            scope.ToString(),
            EmergencySearchCopy.ScopeLabel(
                scope,
                request.CustomerCityName,
                request.CustomerStateName,
                request.CustomerCountryName));
    }

    /// <inheritdoc />
    public void AddTimeline(
        EmergencyRequest request,
        string eventType,
        string message,
        Guid? actorUserId = null,
        object? metadata = null)
    {
        var evt = new EmergencyTimelineEvent
        {
            EmergencyRequestId = request.Id,
            EventType = eventType,
            Message = message,
            ActorUserId = actorUserId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            OccurredAtUtc = _clock.UtcNow
        };
        request.Timeline.Add(evt);
        _dbContext.EmergencyTimelineEvents.Add(evt);
    }

    /// <inheritdoc />
    public async Task WriteHistoryAsync(EmergencyRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.EmergencyHistories
            .AnyAsync(h => h.EmergencyRequestId == request.Id, cancellationToken);
        if (exists)
        {
            return;
        }

        double? acceptanceSeconds = null;
        double? responseSeconds = null;
        if (request.AcceptedAssignmentId.HasValue)
        {
            var accepted = await _dbContext.EmergencyAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.AcceptedAssignmentId.Value, cancellationToken);
            responseSeconds = accepted?.ResponseTimeSeconds;
            if (request.AssignedAtUtc.HasValue && request.CreatedAt != default)
            {
                acceptanceSeconds = (request.AssignedAtUtc.Value - request.CreatedAt).TotalSeconds;
            }
        }

        _dbContext.EmergencyHistories.Add(new EmergencyHistory
        {
            EmergencyRequestId = request.Id,
            CustomerId = request.CustomerId,
            CategoryId = request.CategoryId,
            LawyerId = request.AssignedLawyerId,
            FinalStatus = request.Status,
            AttemptCount = request.AttemptNumber,
            AcceptanceTimeSeconds = acceptanceSeconds,
            ResponseTimeSeconds = responseSeconds,
            CancellationReason = request.CancellationReason,
            ClosedAtUtc = _clock.UtcNow
        });
    }

    private async Task PublishCustomerAsync(
        EmergencyRequest request,
        EmergencyRequestResponse response,
        CancellationToken cancellationToken)
    {
        var customerUserId = request.Customer?.UserId
                             ?? await _dbContext.Customers
                                 .AsNoTracking()
                                 .Where(c => c.Id == request.CustomerId)
                                 .Select(c => c.UserId)
                                 .FirstAsync(cancellationToken);

        await _realtime.PublishStatusAsync(response, cancellationToken);
        await _notifications.NotifyUserAsync(
            customerUserId,
            "Emergency SOS update",
            EmergencySearchCopy.Searching(
                request.CurrentSearchScope,
                request.CustomerCityName,
                request.CustomerStateName,
                request.CustomerCountryName),
            NotificationTypes.EmergencyStatus,
            new Dictionary<string, string>
            {
                ["requestId"] = request.Id.ToString("D"),
                ["status"] = response.StatusName
            },
            cancellationToken);
    }
}
