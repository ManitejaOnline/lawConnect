#pragma warning disable CS1591
using Hangfire;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Features.Payments.Services;

namespace LawConnect.Infrastructure.Payments;

/// <summary>
/// Schedules recurring outbox and SOS round sweep jobs via Hangfire.
/// </summary>
public static class OutboxHangfireRegistration
{
    /// <summary>
    /// Registers recurring background jobs.
    /// </summary>
    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<IOutboxProcessor>(
            "payments-outbox-processor",
            processor => processor.ProcessPendingAsync(CancellationToken.None),
            "*/1 * * * *");

        RecurringJob.AddOrUpdate<IEmergencyTimeoutProcessor>(
            "emergency-round-sweep",
            processor => processor.SweepOverdueRoundsAsync(CancellationToken.None),
            "*/1 * * * *");
    }
}
