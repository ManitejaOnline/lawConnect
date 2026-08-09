using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerDocuments;

/// <summary>
/// Handles <see cref="GetMyLawyerDocumentsQuery"/>.
/// </summary>
public sealed class GetMyLawyerDocumentsQueryHandler
    : IRequestHandler<GetMyLawyerDocumentsQuery, Result<IReadOnlyList<LawyerDocumentResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyLawyerDocumentsQueryHandler"/> class.
    /// </summary>
    public GetMyLawyerDocumentsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LawyerDocumentResponse>>> Handle(
        GetMyLawyerDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var documents = await _dbContext.LawyerDocuments
            .AsNoTracking()
            .Where(d => d.LawyerId == lawyerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var responses = documents
            .Select(document => LawyerDocumentResponse.FromEntity(
                document,
                $"/api/lawyers/me/documents/{document.Id}/file"))
            .ToArray();

        return Result.Success<IReadOnlyList<LawyerDocumentResponse>>(responses);
    }
}
