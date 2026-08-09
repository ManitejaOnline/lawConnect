using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Commands.UpdateAddress;

/// <summary>
/// Handles <see cref="UpdateAddressCommand"/>.
/// </summary>
public sealed class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, Result<AddressResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAddressCommandHandler"/> class.
    /// </summary>
    public UpdateAddressCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, IMapper mapper)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<AddressResponse>> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var entity = await _dbContext.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Address), request.AddressId);

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
            var others = await _dbContext.Addresses
                .Where(a => a.UserId == userId && a.Id != entity.Id && a.IsPrimary)
                .ToListAsync(cancellationToken);
            foreach (var address in others)
            {
                address.IsPrimary = false;
            }
        }

        entity.Line1 = request.Line1.Trim();
        entity.Line2 = string.IsNullOrWhiteSpace(request.Line2) ? null : request.Line2.Trim();
        entity.PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
        entity.Landmark = string.IsNullOrWhiteSpace(request.Landmark) ? null : request.Landmark.Trim();
        entity.CityId = request.CityId;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.IsPrimary = request.IsPrimary;
        entity.Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(_mapper.Map<AddressResponse>(entity));
    }
}
