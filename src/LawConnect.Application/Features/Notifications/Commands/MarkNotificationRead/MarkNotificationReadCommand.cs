using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// Marks a notification as read.
/// </summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="MarkNotificationReadCommand"/>.
/// </summary>
public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkNotificationReadCommandValidator"/> class.
    /// </summary>
    public MarkNotificationReadCommandValidator() => RuleFor(x => x.NotificationId).NotEmpty();
}

/// <summary>
/// Handles <see cref="MarkNotificationReadCommand"/>.
/// </summary>
public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkNotificationReadCommandHandler"/> class.
    /// </summary>
    public MarkNotificationReadCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = _dateTimeProvider.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
