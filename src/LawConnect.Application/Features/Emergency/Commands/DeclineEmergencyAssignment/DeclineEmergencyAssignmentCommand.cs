using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Emergency.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Emergency.Commands.DeclineEmergencyAssignment;

/// <summary>
/// Lawyer declines an incoming SOS assignment.
/// </summary>
public sealed record DeclineEmergencyAssignmentCommand(Guid AssignmentId) : IRequest<Result>;

/// <summary>
/// Validates <see cref="DeclineEmergencyAssignmentCommand"/>.
/// </summary>
public sealed class DeclineEmergencyAssignmentCommandValidator : AbstractValidator<DeclineEmergencyAssignmentCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeclineEmergencyAssignmentCommandValidator"/> class.
    /// </summary>
    public DeclineEmergencyAssignmentCommandValidator() => RuleFor(x => x.AssignmentId).NotEmpty();
}

/// <summary>
/// Handles <see cref="DeclineEmergencyAssignmentCommand"/>.
/// </summary>
public sealed class DeclineEmergencyAssignmentCommandHandler : IRequestHandler<DeclineEmergencyAssignmentCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _clock;
    private readonly IEmergencyDispatchService _dispatch;
    private readonly IEmergencyAcceptCoordinator _acceptCoordinator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeclineEmergencyAssignmentCommandHandler"/> class.
    /// </summary>
    public DeclineEmergencyAssignmentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IDateTimeProvider clock,
        IEmergencyDispatchService dispatch,
        IEmergencyAcceptCoordinator acceptCoordinator)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _clock = clock;
        _dispatch = dispatch;
        _acceptCoordinator = acceptCoordinator;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeclineEmergencyAssignmentCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var assignment = await _dbContext.EmergencyAssignments
            .Include(a => a.EmergencyRequest)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyAssignment), request.AssignmentId);

        if (assignment.LawyerId != lawyer.Id)
        {
            throw new ForbiddenException("You are not allowed to decline this SOS assignment.");
        }

        if (assignment.Status != EmergencyAssignmentStatus.Pending)
        {
            throw new ConflictException("This SOS assignment is no longer pending.");
        }

        var now = _clock.UtcNow;
        assignment.Status = EmergencyAssignmentStatus.Declined;
        assignment.RespondedAtUtc = now;
        assignment.ResponseTimeSeconds = Math.Max(0, (now - assignment.NotifiedAtUtc).TotalSeconds);
        assignment.CancellationReason = "Declined by lawyer.";

        _dispatch.AddTimeline(
            assignment.EmergencyRequest,
            EmergencyTimelineEventTypes.Declined,
            "Lawyer declined the SOS.",
            lawyer.UserId,
            new { AssignmentId = assignment.Id, LawyerId = lawyer.Id });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var requestId = assignment.EmergencyRequestId;
        await _acceptCoordinator.RunExclusiveAsync(
            requestId,
            async () =>
            {
                var tracked = await _dbContext.EmergencyRequests
                    .Include(r => r.Category)
                    .Include(r => r.Customer)
                    .Include(r => r.Assignments).ThenInclude(a => a.Lawyer)
                    .FirstAsync(r => r.Id == requestId, cancellationToken);

                if (!EmergencyLifecycle.IsSearching(tracked.Status))
                {
                    return false;
                }

                var pendingLeft = tracked.Assignments.Any(a => a.Status == EmergencyAssignmentStatus.Pending);
                if (pendingLeft)
                {
                    return false;
                }

                await _dispatch.EscalateRoundAsync(
                    tracked,
                    "All notified lawyers declined.",
                    cancellationToken);
                return true;
            },
            cancellationToken);

        return Result.Success();
    }
}
