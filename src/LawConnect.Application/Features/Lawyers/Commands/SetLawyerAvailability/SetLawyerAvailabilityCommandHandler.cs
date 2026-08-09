using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Commands.SetLawyerAvailability;

/// <summary>
/// Handles <see cref="SetLawyerAvailabilityCommand"/>.
/// </summary>
public sealed class SetLawyerAvailabilityCommandHandler
    : IRequestHandler<SetLawyerAvailabilityCommand, Result<IReadOnlyList<LawyerAvailabilityResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ILawyerProfileCache _cache;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetLawyerAvailabilityCommandHandler"/> class.
    /// </summary>
    public SetLawyerAvailabilityCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        ILawyerProfileCache cache,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _cache = cache;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LawyerAvailabilityResponse>>> Handle(
        SetLawyerAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        var existing = await _dbContext.LawyerAvailabilities
            .Where(a => a.LawyerId == lawyer.Id)
            .ToListAsync(cancellationToken);
        _dbContext.LawyerAvailabilities.RemoveRange(existing);

        var created = new List<LawyerAvailability>();
        foreach (var slot in request.Slots)
        {
            var entity = new LawyerAvailability
            {
                LawyerId = lawyer.Id,
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                IsAvailable = slot.IsAvailable,
                TimeZoneId = slot.TimeZoneId.Trim()
            };
            created.Add(entity);
            _dbContext.LawyerAvailabilities.Add(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);
        return Result.Success<IReadOnlyList<LawyerAvailabilityResponse>>(
            _mapper.Map<List<LawyerAvailabilityResponse>>(created));
    }
}
