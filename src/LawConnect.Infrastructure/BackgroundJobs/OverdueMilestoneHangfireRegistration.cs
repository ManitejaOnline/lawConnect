#pragma warning disable CS1591
using Hangfire;
using LawConnect.Application.Features.Cases.Services;

namespace LawConnect.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules the periodic overdue-milestone sweep via Hangfire — the only mechanism allowed to set
/// a milestone's status to Overdue.
/// </summary>
public static class OverdueMilestoneHangfireRegistration
{
    /// <summary>
    /// Registers the recurring overdue-milestone sweep job.
    /// </summary>
    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<IOverdueMilestoneSweepService>(
            "overdue-milestone-sweep",
            service => service.SweepAsync(CancellationToken.None),
            "0 * * * *");
    }
}
