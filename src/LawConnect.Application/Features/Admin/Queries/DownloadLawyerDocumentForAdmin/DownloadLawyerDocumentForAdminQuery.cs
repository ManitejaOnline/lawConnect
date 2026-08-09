using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Queries.DownloadLawyerDocument;
using MediatR;

namespace LawConnect.Application.Features.Admin.Queries.DownloadLawyerDocumentForAdmin;

/// <summary>
/// Downloads a lawyer verification document for an administrator.
/// </summary>
public sealed record DownloadLawyerDocumentForAdminQuery(Guid LawyerId, Guid DocumentId)
    : IRequest<Result<LawyerDocumentFile>>;
