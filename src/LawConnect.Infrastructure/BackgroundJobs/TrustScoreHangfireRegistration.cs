#pragma warning disable CS1591
using Hangfire;
using LawConnect.Application.Features.Lawyers.Services;

namespace LawConnect.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules the periodic Trust Score recalculation sweep via Hangfire. Discrete important events
/// (rating submitted, appointment resolved, complaint reviewed) trigger an immediate recalculation
/// inline in their own command handlers; this sweep is the safety net that picks up drift from
/// high-frequency signals like ordinary chat activity without recalculating on every message.
/// </summary>
public static class TrustScoreHangfireRegistration
{
    /// <summary>
    /// Registers the recurring Trust Score sweep job.
    /// </summary>
    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<ITrustScoreService>(
            "trust-score-recalc-sweep",
            service => service.RecalculateAllApprovedAsync(CancellationToken.None),
            "*/30 * * * *");
    }
}
