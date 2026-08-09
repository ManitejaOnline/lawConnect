using AutoMapper;
using AutoMapper.QueryableExtensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerAvailability;

/// <summary>
/// Handles <see cref="GetMyLawyerAvailabilityQuery"/>.
/// </summary>
public sealed class GetMyLawyerAvailabilityQueryHandler
    : IRequestHandler<GetMyLawyerAvailabilityQuery, Result<IReadOnlyList<LawyerAvailabilityResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyLawyerAvailabilityQueryHandler"/> class.
    /// </summary>
    public GetMyLawyerAvailabilityQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LawyerAvailabilityResponse>>> Handle(
        GetMyLawyerAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var slots = await _dbContext.LawyerAvailabilities
            .AsNoTracking()
            .Where(a => a.LawyerId == lawyerId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ProjectTo<LawyerAvailabilityResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LawyerAvailabilityResponse>>(slots);
    }
}
