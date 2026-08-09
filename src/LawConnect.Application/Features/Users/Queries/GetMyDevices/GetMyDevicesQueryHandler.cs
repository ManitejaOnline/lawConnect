using AutoMapper;
using AutoMapper.QueryableExtensions;
using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Specifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Queries.GetMyDevices;

/// <summary>
/// Handles <see cref="GetMyDevicesQuery"/>.
/// </summary>
public sealed class GetMyDevicesQueryHandler : IRequestHandler<GetMyDevicesQuery, Result<IReadOnlyList<DeviceResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyDevicesQueryHandler"/> class.
    /// </summary>
    public GetMyDevicesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, IMapper mapper)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DeviceResponse>>> Handle(
        GetMyDevicesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var devices = await _dbContext.Devices
            .AsQueryable()
            .ApplySpecification(new UserDevicesByUserIdSpec(userId))
            .ProjectTo<DeviceResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DeviceResponse>>(devices);
    }
}
