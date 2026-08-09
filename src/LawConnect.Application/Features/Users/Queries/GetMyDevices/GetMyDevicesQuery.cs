using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using MediatR;

namespace LawConnect.Application.Features.Users.Queries.GetMyDevices;

/// <summary>
/// Returns devices for the current user.
/// </summary>
public sealed record GetMyDevicesQuery : IRequest<Result<IReadOnlyList<DeviceResponse>>>;
