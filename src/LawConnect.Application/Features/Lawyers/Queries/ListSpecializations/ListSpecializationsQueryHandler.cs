using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.ListSpecializations;

/// <summary>
/// Handles <see cref="ListSpecializationsQuery"/>.
/// </summary>
public sealed class ListSpecializationsQueryHandler
    : IRequestHandler<ListSpecializationsQuery, Result<IReadOnlyList<SpecializationOptionResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSpecializationsQueryHandler"/> class.
    /// </summary>
    public ListSpecializationsQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SpecializationOptionResponse>>> Handle(
        ListSpecializationsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.Specializations
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Select(s => new SpecializationOptionResponse(s.Id, s.Name, s.Slug ?? string.Empty))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<SpecializationOptionResponse>>(items);
    }
}
