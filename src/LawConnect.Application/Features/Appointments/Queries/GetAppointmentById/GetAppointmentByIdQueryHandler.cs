using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Appointments.Models;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Appointments.Queries.GetAppointmentById;

/// <summary>
/// Handles <see cref="GetAppointmentByIdQuery"/>.
/// </summary>
public sealed class GetAppointmentByIdQueryHandler
    : IRequestHandler<GetAppointmentByIdQuery, Result<AppointmentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAppointmentByIdQueryHandler"/> class.
    /// </summary>
    public GetAppointmentByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<AppointmentResponse>> Handle(
        GetAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer).ThenInclude(c => c.User)
            .Include(a => a.Lawyer).ThenInclude(l => l.User)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.AppointmentId);

        if (appointment.Customer.UserId != userId && appointment.Lawyer.UserId != userId)
        {
            throw new ForbiddenException("You are not allowed to view this appointment.");
        }

        return Result.Success(AppointmentMapper.ToResponse(appointment));
    }
}
