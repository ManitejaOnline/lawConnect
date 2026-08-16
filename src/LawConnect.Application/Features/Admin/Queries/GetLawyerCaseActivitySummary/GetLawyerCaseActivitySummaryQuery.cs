using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.GetLawyerCaseActivitySummary;

/// <summary>
/// Computes objective, aggregate case-management signals for one lawyer across all their cases.
/// This is the "new Case Management data provider" that can later feed the Trust Score system —
/// it is intentionally read-only and not wired into <c>TrustScoreCalculator</c>/
/// <c>TrustScoreService</c> in this change, since those are frozen this session. Exposed to admins
/// now for overdue/inactive-case visibility.
/// </summary>
public sealed record GetLawyerCaseActivitySummaryQuery(Guid LawyerId) : IRequest<Result<LawyerCaseActivitySummaryResponse>>;
