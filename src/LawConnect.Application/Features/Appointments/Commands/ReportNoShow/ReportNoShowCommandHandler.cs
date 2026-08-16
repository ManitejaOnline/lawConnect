using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Appointments.Commands.ReportNoShow;

/// <summary>
/// Handles <see cref="ReportNoShowCommand"/>.
/// </summary>
public sealed class ReportNoShowCommandHandler : IRequestHandler<ReportNoShowCommand, Result<AppointmentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITrustScoreService _trustScoreService;
    private readonly ILogger<ReportNoShowCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportNoShowCommandHandler"/> class.
    /// </summary>
    public ReportNoShowCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ITrustScoreService trustScoreService,
        ILogger<ReportNoShowCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _trustScoreService = trustScoreService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AppointmentResponse>> Handle(ReportNoShowCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var appointment = await _dbContext.Appointments
            .Include(a => a.Customer).ThenInclude(c => c.User)
            .Include(a => a.Lawyer).ThenInclude(l => l.User)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.AppointmentId);

        if (appointment.Customer.UserId != userId)
        {
            throw new ForbiddenException("Only the customer on this appointment can report a no-show.");
        }

        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            throw new BusinessRuleException(
                "Only a still-scheduled appointment can be reported as a no-show.",
                "Appointment.InvalidStatusForNoShow");
        }

        var now = _dateTimeProvider.UtcNow;
        if (appointment.EndUtc > now)
        {
            throw new BusinessRuleException(
                "A no-show can only be reported after the scheduled end time.",
                "Appointment.NotYetOccurred");
        }

        appointment.Status = AppointmentStatus.NoShow;
        appointment.NoShowReportedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Appointment {AppointmentId} reported as no-show by customer {UserId} for lawyer {LawyerId}",
            appointment.Id,
            userId,
            appointment.LawyerId);

        await _trustScoreService.RecalculateAsync(appointment.LawyerId, cancellationToken);

        return Result.Success(AppointmentMapper.ToResponse(appointment));
    }
}
