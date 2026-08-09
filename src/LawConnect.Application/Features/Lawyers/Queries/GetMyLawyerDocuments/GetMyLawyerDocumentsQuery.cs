using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerDocuments;

/// <summary>
/// Returns verification documents for the current lawyer.
/// </summary>
public sealed record GetMyLawyerDocumentsQuery : IRequest<Result<IReadOnlyList<LawyerDocumentResponse>>>;
