using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.DownloadLawyerDocument;

/// <summary>
/// Downloads a verification document for the owning lawyer.
/// </summary>
public sealed record DownloadLawyerDocumentQuery(Guid DocumentId) : IRequest<Result<LawyerDocumentFile>>;

/// <summary>
/// File payload for an authorized document download.
/// </summary>
/// <param name="FileName">Original file name.</param>
/// <param name="ContentType">MIME type.</param>
/// <param name="Content">Readable content stream. Caller disposes.</param>
public sealed record LawyerDocumentFile(string FileName, string ContentType, Stream Content);
