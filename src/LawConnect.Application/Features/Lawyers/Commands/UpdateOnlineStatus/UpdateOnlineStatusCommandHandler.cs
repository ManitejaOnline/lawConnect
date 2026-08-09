using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Lawyers.Commands.UpdateOnlineStatus;

/// <summary>
/// Handles <see cref="UpdateOnlineStatusCommand"/>.
/// </summary>
public sealed class UpdateOnlineStatusCommandHandler
    : IRequestHandler<UpdateOnlineStatusCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILawyerProfileCache _cache;
    private readonly ILogger<UpdateOnlineStatusCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateOnlineStatusCommandHandler"/> class.
    /// </summary>
    public UpdateOnlineStatusCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IDateTimeProvider dateTimeProvider,
        ILawyerProfileCache cache,
        ILogger<UpdateOnlineStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _dateTimeProvider = dateTimeProvider;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerProfileResponse>> Handle(
        UpdateOnlineStatusCommand request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);
        if (request.IsOnline)
        {
            LawyerOperationalRules.EnsureApproved(lawyer);
        }

        lawyer.IsOnline = request.IsOnline;
        if (request.IsOnline)
        {
            lawyer.LastOnlineAtUtc = _dateTimeProvider.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(lawyer.Id, cancellationToken);
        _logger.LogInformation("Lawyer {LawyerId} online status set to {IsOnline}", lawyer.Id, request.IsOnline);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);
        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }
}
