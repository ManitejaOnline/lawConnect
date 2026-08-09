using AutoMapper;
using AutoMapper.QueryableExtensions;
using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Specifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Queries.GetMyAddresses;

/// <summary>
/// Handles <see cref="GetMyAddressesQuery"/>.
/// </summary>
public sealed class GetMyAddressesQueryHandler
    : IRequestHandler<GetMyAddressesQuery, Result<IReadOnlyList<AddressResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyAddressesQueryHandler"/> class.
    /// </summary>
    public GetMyAddressesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, IMapper mapper)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AddressResponse>>> Handle(
        GetMyAddressesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var addresses = await _dbContext.Addresses
            .AsQueryable()
            .ApplySpecification(new UserAddressesByUserIdSpec(userId))
            .ProjectTo<AddressResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AddressResponse>>(addresses);
    }
}
