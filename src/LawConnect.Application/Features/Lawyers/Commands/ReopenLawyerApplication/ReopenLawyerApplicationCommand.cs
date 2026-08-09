using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Commands.ReopenLawyerApplication;

/// <summary>
/// Reopens a rejected application so the lawyer can resubmit.
/// </summary>
public sealed record ReopenLawyerApplicationCommand : IRequest<Result<LawyerProfileResponse>>;
