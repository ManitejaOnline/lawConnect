using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Queries.GetMyAddresses;

/// <summary>
/// Returns addresses for the current user.
/// </summary>
public sealed record GetMyAddressesQuery : IRequest<Result<IReadOnlyList<AddressResponse>>>;
