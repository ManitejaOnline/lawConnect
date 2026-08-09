using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.GetLawyerApplication;

/// <summary>
/// Loads one lawyer application for admin review.
/// </summary>
public sealed record GetLawyerApplicationQuery(Guid LawyerId) : IRequest<Result<AdminLawyerApplicationDetail>>;
