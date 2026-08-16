using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Cases.Services;

/// <summary>
/// Flags case milestones as Overdue once their due date has passed. Runs periodically via
/// Hangfire — never invoked from a client-facing command, since only the sweep is allowed to set
/// <see cref="MilestoneStatus.Overdue"/>.
/// </summary>
public interface IOverdueMilestoneSweepService
{
    /// <summary>Flags all Pending/InProgress milestones past their due date as Overdue.</summary>
    Task SweepAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IOverdueMilestoneSweepService" />
public sealed class OverdueMilestoneSweepService : IOverdueMilestoneSweepService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<OverdueMilestoneSweepService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OverdueMilestoneSweepService"/> class.
    /// </summary>
    public OverdueMilestoneSweepService(IApplicationDbContext dbContext, IDateTimeProvider clock, ILogger<OverdueMilestoneSweepService> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SweepAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var overdue = await _dbContext.CaseMilestones
            .Where(m => (m.Status == MilestoneStatus.Pending || m.Status == MilestoneStatus.InProgress)
                && m.DueDate.HasValue && m.DueDate.Value < now)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var milestone in overdue)
        {
            milestone.Status = MilestoneStatus.Overdue;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Overdue milestone sweep flagged {Count} milestones", overdue.Count);
    }
}
