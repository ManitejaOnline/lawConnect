using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Commands.DeactivateDevice;

/// <summary>
/// Handles <see cref="DeactivateDeviceCommand"/>.
/// </summary>
public sealed class DeactivateDeviceCommandHandler : IRequestHandler<DeactivateDeviceCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeactivateDeviceCommandHandler"/> class.
    /// </summary>
    public DeactivateDeviceCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateDeviceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var device = await _dbContext.Devices.FirstOrDefaultAsync(
            d => d.UserId == userId && d.DeviceIdentifier == request.DeviceIdentifier,
            cancellationToken)
            ?? throw new NotFoundException(nameof(Device), request.DeviceIdentifier);

        device.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
