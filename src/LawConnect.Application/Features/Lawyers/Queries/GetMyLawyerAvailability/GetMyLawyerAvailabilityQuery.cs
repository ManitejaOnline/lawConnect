using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerAvailability;

/// <summary>
/// Returns availability slots for the current lawyer.
/// </summary>
public sealed record GetMyLawyerAvailabilityQuery : IRequest<Result<IReadOnlyList<LawyerAvailabilityResponse>>>;
