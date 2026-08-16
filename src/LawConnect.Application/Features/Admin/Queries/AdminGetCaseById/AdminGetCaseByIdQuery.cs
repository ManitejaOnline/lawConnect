using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.AdminGetCaseById;

/// <summary>
/// Gets full case detail for admin — unscoped, any case.
/// </summary>
public sealed record AdminGetCaseByIdQuery(Guid CaseId) : IRequest<Result<AdminCaseDetailResponse>>;
