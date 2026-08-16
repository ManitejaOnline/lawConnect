using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Cases.Models;
using MediatR;

namespace LawConnect.Application.Features.Cases.Queries.ListCaseDocuments;

/// <summary>
/// Lists all files attached to a case, newest first.
/// </summary>
public sealed record ListCaseDocumentsQuery(Guid CaseId) : IRequest<Result<IReadOnlyList<CaseDocumentResponse>>>;
