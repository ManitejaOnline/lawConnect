using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Emergency.Models;
using LawConnect.Application.Features.Emergency.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Emergency.Queries;

/// <summary>Lists active emergency categories.</summary>
public sealed record GetEmergencyCategoriesQuery : IRequest<Result<IReadOnlyList<EmergencyCategoryResponse>>>;

/// <summary>Handles <see cref="GetEmergencyCategoriesQuery"/>.</summary>
public sealed class GetEmergencyCategoriesQueryHandler
    : IRequestHandler<GetEmergencyCategoriesQuery, Result<IReadOnlyList<EmergencyCategoryResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="GetEmergencyCategoriesQueryHandler"/> class.</summary>
    public GetEmergencyCategoriesQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EmergencyCategoryResponse>>> Handle(
        GetEmergencyCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.EmergencyCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new EmergencyCategoryResponse(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.SpecializationId,
                c.Specialization.Name,
                c.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EmergencyCategoryResponse>>(items);
    }
}

/// <summary>Gets an SOS request by id for participants.</summary>
public sealed record GetEmergencyRequestQuery(Guid RequestId) : IRequest<Result<EmergencyRequestResponse>>;

/// <summary>Handles <see cref="GetEmergencyRequestQuery"/>.</summary>
public sealed class GetEmergencyRequestQueryHandler
    : IRequestHandler<GetEmergencyRequestQuery, Result<EmergencyRequestResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEmergencyDispatchService _dispatch;

    /// <summary>Initializes a new instance of the <see cref="GetEmergencyRequestQueryHandler"/> class.</summary>
    public GetEmergencyRequestQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IEmergencyDispatchService dispatch)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dispatch = dispatch;
    }

    /// <inheritdoc />
    public async Task<Result<EmergencyRequestResponse>> Handle(
        GetEmergencyRequestQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var sos = await _dbContext.EmergencyRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyRequest), request.RequestId);

        var isCustomer = sos.Customer.UserId == userId;
        var isAssignedLawyer = false;
        if (sos.AssignedLawyerId.HasValue)
        {
            isAssignedLawyer = await _dbContext.Lawyers
                .AsNoTracking()
                .AnyAsync(l => l.Id == sos.AssignedLawyerId && l.UserId == userId, cancellationToken);
        }

        var isNotifiedLawyer = await _dbContext.EmergencyAssignments
            .AsNoTracking()
            .AnyAsync(
                a => a.EmergencyRequestId == sos.Id
                     && a.Lawyer.UserId == userId
                     && (a.Status == EmergencyAssignmentStatus.Pending || a.Status == EmergencyAssignmentStatus.Accepted),
                cancellationToken);

        if (!isCustomer && !isAssignedLawyer && !isNotifiedLawyer)
        {
            throw new ForbiddenException("You are not allowed to view this SOS request.");
        }

        return Result.Success(_dispatch.ToResponse(sos, sos.Category.Name));
    }
}

/// <summary>Gets SOS timeline events.</summary>
public sealed record GetEmergencyTimelineQuery(Guid RequestId)
    : IRequest<Result<IReadOnlyList<EmergencyTimelineItemResponse>>>;

/// <summary>Handles <see cref="GetEmergencyTimelineQuery"/>.</summary>
public sealed class GetEmergencyTimelineQueryHandler
    : IRequestHandler<GetEmergencyTimelineQuery, Result<IReadOnlyList<EmergencyTimelineItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="GetEmergencyTimelineQueryHandler"/> class.</summary>
    public GetEmergencyTimelineQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EmergencyTimelineItemResponse>>> Handle(
        GetEmergencyTimelineQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var sos = await _dbContext.EmergencyRequests
            .AsNoTracking()
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyRequest), request.RequestId);

        var allowed = sos.Customer.UserId == userId
                      || await _dbContext.EmergencyAssignments.AsNoTracking()
                          .AnyAsync(
                              a => a.EmergencyRequestId == sos.Id
                                   && a.Lawyer.UserId == userId
                                   && (a.Status == EmergencyAssignmentStatus.Pending || a.Status == EmergencyAssignmentStatus.Accepted),
                              cancellationToken);
        if (!allowed)
        {
            throw new ForbiddenException("You are not allowed to view this SOS timeline.");
        }

        var items = await _dbContext.EmergencyTimelineEvents
            .AsNoTracking()
            .Where(t => t.EmergencyRequestId == request.RequestId)
            .OrderBy(t => t.OccurredAtUtc)
            .Select(t => new EmergencyTimelineItemResponse(
                t.Id,
                t.EventType,
                t.Message,
                t.ActorUserId,
                t.MetadataJson,
                t.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EmergencyTimelineItemResponse>>(items);
    }
}

/// <summary>Lists pending SOS assignments for the current lawyer.</summary>
public sealed record GetIncomingEmergencyAssignmentsQuery
    : IRequest<Result<IReadOnlyList<IncomingEmergencySosResponse>>>;

/// <summary>Handles <see cref="GetIncomingEmergencyAssignmentsQuery"/>.</summary>
public sealed class GetIncomingEmergencyAssignmentsQueryHandler
    : IRequestHandler<GetIncomingEmergencyAssignmentsQuery, Result<IReadOnlyList<IncomingEmergencySosResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _clock;

    /// <summary>Initializes a new instance of the <see cref="GetIncomingEmergencyAssignmentsQueryHandler"/> class.</summary>
    public GetIncomingEmergencyAssignmentsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IncomingEmergencySosResponse>>> Handle(
        GetIncomingEmergencyAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var now = _clock.UtcNow;
        var items = await _dbContext.EmergencyAssignments
            .AsNoTracking()
            .Where(a =>
                a.LawyerId == lawyer.Id
                && a.Status == EmergencyAssignmentStatus.Pending
                && (a.EmergencyRequest.Status == EmergencyStatus.Created
                    || a.EmergencyRequest.Status == EmergencyStatus.Searching
                    || a.EmergencyRequest.Status == EmergencyStatus.Notifying))
            .OrderBy(a => a.NotifiedAtUtc)
            .Select(a => new IncomingEmergencySosResponse(
                a.EmergencyRequestId,
                a.Id,
                a.EmergencyRequest.CategoryId,
                a.EmergencyRequest.Category.Name,
                a.EmergencyRequest.Latitude,
                a.EmergencyRequest.Longitude,
                a.DistanceKm,
                a.Rank,
                a.AttemptNumber,
                a.EmergencyRequest.SearchExpiresAtUtc ?? now,
                a.EmergencyRequest.SearchExpiresAtUtc == null
                    ? 0
                    : (int)Math.Max(0, Math.Ceiling((a.EmergencyRequest.SearchExpiresAtUtc.Value - now).TotalSeconds)),
                a.NotifiedAtUtc,
                a.EmergencyRequest.CustomerCityName))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<IncomingEmergencySosResponse>>(items);
    }
}

/// <summary>Lists SOS history for the current customer or lawyer.</summary>
public sealed record GetMyEmergencyHistoryQuery(int PageNumber, int PageSize)
    : IRequest<Result<PagedResult<EmergencyHistoryItemResponse>>>;

/// <summary>Handles <see cref="GetMyEmergencyHistoryQuery"/>.</summary>
public sealed class GetMyEmergencyHistoryQueryHandler
    : IRequestHandler<GetMyEmergencyHistoryQuery, Result<PagedResult<EmergencyHistoryItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="GetMyEmergencyHistoryQueryHandler"/> class.</summary>
    public GetMyEmergencyHistoryQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<EmergencyHistoryItemResponse>>> Handle(
        GetMyEmergencyHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var customerId = await _dbContext.Customers.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var lawyerId = await _dbContext.Lawyers.AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerId is null && lawyerId is null)
        {
            throw new ForbiddenException("You are not allowed to view emergency history.");
        }

        var pagination = new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize };
        var query = _dbContext.EmergencyHistories.AsNoTracking().Where(h =>
            (customerId != null && h.CustomerId == customerId)
            || (lawyerId != null && h.LawyerId == lawyerId));
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(h => h.ClosedAtUtc)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(h => new EmergencyHistoryItemResponse(
                h.Id,
                h.EmergencyRequestId,
                h.CategoryId,
                h.LawyerId,
                h.FinalStatus.ToString(),
                h.AttemptCount,
                h.AcceptanceTimeSeconds,
                h.ResponseTimeSeconds,
                h.CancellationReason,
                h.ClosedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<EmergencyHistoryItemResponse>(
            items,
            total,
            pagination.PageNumber,
            pagination.PageSize));
    }
}

/// <summary>Gets aggregate SOS metrics.</summary>
public sealed record GetEmergencyMetricsQuery : IRequest<Result<EmergencyMetricsResponse>>;

/// <summary>Handles <see cref="GetEmergencyMetricsQuery"/>.</summary>
public sealed class GetEmergencyMetricsQueryHandler
    : IRequestHandler<GetEmergencyMetricsQuery, Result<EmergencyMetricsResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>Initializes a new instance of the <see cref="GetEmergencyMetricsQueryHandler"/> class.</summary>
    public GetEmergencyMetricsQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Result<EmergencyMetricsResponse>> Handle(
        GetEmergencyMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var histories = await _dbContext.EmergencyHistories.AsNoTracking().ToListAsync(cancellationToken);
        var total = histories.Count;
        var completed = histories.Count(h => h.FinalStatus == EmergencyStatus.Completed);
        var cancelled = histories.Count(h => h.FinalStatus == EmergencyStatus.Cancelled);
        var expired = histories.Count(h => h.FinalStatus == EmergencyStatus.Expired);
        var failed = histories.Count(h => h.FinalStatus == EmergencyStatus.Failed);
        var accepted = histories.Count(h => h.LawyerId.HasValue);
        var avgResponse = histories
            .Where(h => h.ResponseTimeSeconds.HasValue)
            .Select(h => h.ResponseTimeSeconds!.Value)
            .DefaultIfEmpty()
            .Average();
        var avgAcceptance = histories
            .Where(h => h.AcceptanceTimeSeconds.HasValue)
            .Select(h => h.AcceptanceTimeSeconds!.Value)
            .DefaultIfEmpty()
            .Average();

        var response = new EmergencyMetricsResponse(
            total,
            completed,
            cancelled,
            expired,
            failed,
            total == 0 ? 0 : completed * 100d / total,
            total == 0 ? 0 : accepted * 100d / total,
            histories.Any(h => h.ResponseTimeSeconds.HasValue) ? avgResponse : null,
            histories.Any(h => h.AcceptanceTimeSeconds.HasValue) ? avgAcceptance : null);

        return Result.Success(response);
    }
}

/// <summary>Gets the current customer's open SOS, if any.</summary>
public sealed record GetActiveEmergencySosQuery : IRequest<Result<EmergencyRequestResponse?>>;

/// <summary>Handles <see cref="GetActiveEmergencySosQuery"/>.</summary>
public sealed class GetActiveEmergencySosQueryHandler
    : IRequestHandler<GetActiveEmergencySosQuery, Result<EmergencyRequestResponse?>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly IEmergencyDispatchService _dispatch;

    /// <summary>Initializes a new instance of the <see cref="GetActiveEmergencySosQueryHandler"/> class.</summary>
    public GetActiveEmergencySosQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentCustomerService currentCustomer,
        IEmergencyDispatchService dispatch)
    {
        _dbContext = dbContext;
        _currentCustomer = currentCustomer;
        _dispatch = dispatch;
    }

    /// <inheritdoc />
    public async Task<Result<EmergencyRequestResponse?>> Handle(
        GetActiveEmergencySosQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _currentCustomer.GetOrCreateAsync(cancellationToken);
        var open = EmergencyLifecycle.Open;
        var sos = await _dbContext.EmergencyRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Customer)
            .Where(r => r.CustomerId == customer.Id && open.Contains(r.Status))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(sos is null ? null : _dispatch.ToResponse(sos, sos.Category.Name));
    }
}

/// <summary>Gets the current lawyer's assigned open SOS, if any.</summary>
public sealed record GetLawyerActiveEmergencyQuery : IRequest<Result<EmergencyRequestResponse?>>;

/// <summary>Handles <see cref="GetLawyerActiveEmergencyQuery"/>.</summary>
public sealed class GetLawyerActiveEmergencyQueryHandler
    : IRequestHandler<GetLawyerActiveEmergencyQuery, Result<EmergencyRequestResponse?>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IEmergencyDispatchService _dispatch;

    /// <summary>Initializes a new instance of the <see cref="GetLawyerActiveEmergencyQueryHandler"/> class.</summary>
    public GetLawyerActiveEmergencyQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IEmergencyDispatchService dispatch)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _dispatch = dispatch;
    }

    /// <inheritdoc />
    public async Task<Result<EmergencyRequestResponse?>> Handle(
        GetLawyerActiveEmergencyQuery request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var assigned = EmergencyLifecycle.AssignedLike;
        var sos = await _dbContext.EmergencyRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Customer)
            .Where(r => r.AssignedLawyerId == lawyer.Id && assigned.Contains(r.Status))
            .OrderByDescending(r => r.AssignedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(sos is null ? null : _dispatch.ToResponse(sos, sos.Category.Name));
    }
}
