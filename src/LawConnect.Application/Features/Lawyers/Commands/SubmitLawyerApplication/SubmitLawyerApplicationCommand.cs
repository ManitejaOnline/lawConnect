using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerApplication;

/// <summary>
/// Submits the current lawyer application for admin verification.
/// </summary>
public sealed record SubmitLawyerApplicationCommand : IRequest<Result<LawyerProfileResponse>>;
