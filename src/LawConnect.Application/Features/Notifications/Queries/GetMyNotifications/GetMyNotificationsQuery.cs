using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Chat.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Notifications.Queries.GetMyNotifications;

/// <summary>
/// Lists notification history for the current user.
/// </summary>
public sealed record GetMyNotificationsQuery(int PageNumber, int PageSize, bool UnreadOnly)
    : IRequest<Result<PagedResult<NotificationItemResponse>>>;

/// <summary>
/// Validates <see cref="GetMyNotificationsQuery"/>.
/// </summary>
public sealed class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNotificationsQueryValidator"/> class.
    /// </summary>
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationRequest.MaxPageSize);
    }
}

/// <summary>
/// Handles <see cref="GetMyNotificationsQuery"/>.
/// </summary>
public sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, Result<PagedResult<NotificationItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNotificationsQueryHandler"/> class.
    /// </summary>
    public GetMyNotificationsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<NotificationItemResponse>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var pagination = new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize };

        var query = _dbContext.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(n => new NotificationItemResponse(
                n.Id,
                n.Title,
                n.Body,
                n.Type,
                n.Channel.ToString(),
                n.IsRead,
                n.CreatedAt,
                n.DataJson))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<NotificationItemResponse>(
            items,
            total,
            pagination.PageNumber,
            pagination.PageSize));
    }
}
