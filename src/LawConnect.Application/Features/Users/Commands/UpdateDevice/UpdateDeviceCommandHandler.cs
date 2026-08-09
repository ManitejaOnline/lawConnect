using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Commands.UpdateDevice;

/// <summary>
/// Handles <see cref="UpdateDeviceCommand"/>.
/// </summary>
public sealed class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand, Result<DeviceResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDeviceCommandHandler"/> class.
    /// </summary>
    public UpdateDeviceCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<DeviceResponse>> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var device = await _dbContext.Devices.FirstOrDefaultAsync(
            d => d.UserId == userId && d.DeviceIdentifier == request.DeviceIdentifier,
            cancellationToken)
            ?? throw new NotFoundException(nameof(Device), request.DeviceIdentifier);

        if (request.Platform.HasValue)
        {
            device.Platform = request.Platform.Value;
        }

        device.FcmToken = string.IsNullOrWhiteSpace(request.FcmToken) ? device.FcmToken : request.FcmToken.Trim();
        device.AppVersion = string.IsNullOrWhiteSpace(request.AppVersion) ? device.AppVersion : request.AppVersion.Trim();
        device.OsVersion = string.IsNullOrWhiteSpace(request.OsVersion) ? device.OsVersion : request.OsVersion.Trim();
        device.IsActive = true;
        device.LastSeenAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(_mapper.Map<DeviceResponse>(device));
    }
}
