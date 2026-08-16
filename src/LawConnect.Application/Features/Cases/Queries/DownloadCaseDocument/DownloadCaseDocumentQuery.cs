using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Queries.DownloadCaseDocument;

/// <summary>
/// Downloads a file attached to a case, for a user with read access to the case.
/// </summary>
public sealed record DownloadCaseDocumentQuery(Guid CaseId, Guid DocumentId) : IRequest<Result<CaseDocumentFile>>;

/// <summary>
/// File payload for an authorized case document download.
/// </summary>
/// <param name="FileName">Original file name.</param>
/// <param name="ContentType">MIME type.</param>
/// <param name="Content">Readable content stream. Caller disposes.</param>
public sealed record CaseDocumentFile(string FileName, string ContentType, Stream Content);
