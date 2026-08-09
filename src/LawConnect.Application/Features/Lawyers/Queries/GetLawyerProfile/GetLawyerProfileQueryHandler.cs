using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerProfile;

/// <summary>
/// Handles <see cref="GetLawyerProfileQuery"/> with short-lived caching.
/// </summary>
public sealed class GetLawyerProfileQueryHandler
    : IRequestHandler<GetLawyerProfileQuery, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILawyerProfileCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetLawyerProfileQueryHandler"/> class.
    /// </summary>
    public GetLawyerProfileQueryHandler(IApplicationDbContext dbContext, ILawyerProfileCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerProfileResponse>> Handle(
        GetLawyerProfileQuery request,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync(request.LawyerId, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var lawyer = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(request.LawyerId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Lawyer), request.LawyerId);

        if (!LawyerOperationalRules.IsApproved(lawyer))
        {
            throw new NotFoundException(nameof(Lawyer), request.LawyerId);
        }

        var response = LawyerProfileMapper.ToProfileResponse(lawyer);
        await _cache.SetAsync(response, cancellationToken);
        return Result.Success(response);
    }
}
