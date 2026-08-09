using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Commands.CreateAddress;

/// <summary>
/// Handles <see cref="CreateAddressCommand"/>.
/// </summary>
public sealed class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, Result<AddressResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILocationResolver _locationResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAddressCommandHandler"/> class.
    /// </summary>
    public CreateAddressCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IMapper mapper,
        ILocationResolver locationResolver)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mapper = mapper;
        _locationResolver = locationResolver;
    }

    /// <inheritdoc />
    public async Task<Result<AddressResponse>> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        if (request.CityId.HasValue)
        {
            var cityExists = await _dbContext.Cities.AnyAsync(c => c.Id == request.CityId, cancellationToken);
            if (!cityExists)
            {
                throw new NotFoundException(nameof(City), request.CityId.Value);
            }
        }

        if (request.IsPrimary)
        {
            var existing = await _dbContext.Addresses.Where(a => a.UserId == userId && a.IsPrimary).ToListAsync(cancellationToken);
            foreach (var address in existing)
            {
                address.IsPrimary = false;
            }
        }

        var cityId = request.CityId;
        if (!cityId.HasValue && request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var place = await _locationResolver.ResolveFromCoordinatesAsync(
                request.Latitude.Value,
                request.Longitude.Value,
                cancellationToken);
            cityId = place?.CityId;
        }

        var entity = new Address
        {
            UserId = userId,
            Line1 = request.Line1.Trim(),
            Line2 = string.IsNullOrWhiteSpace(request.Line2) ? null : request.Line2.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim(),
            Landmark = string.IsNullOrWhiteSpace(request.Landmark) ? null : request.Landmark.Trim(),
            CityId = cityId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsPrimary = request.IsPrimary,
            Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim()
        };

        _dbContext.Addresses.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(_mapper.Map<AddressResponse>(entity));
    }
}
