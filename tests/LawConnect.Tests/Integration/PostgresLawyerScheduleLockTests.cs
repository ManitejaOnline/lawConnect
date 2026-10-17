using LawConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Tests.Integration;

/// <summary>
/// Proves the booking lock serializes work on the local PostgreSQL instance.
/// The in-memory test database skips this lock.
/// </summary>
public sealed class PostgresLawyerScheduleLockTests
{
    [Fact]
    public async Task AdvisoryLock_SerializesWorkForTheSameLawyer()
    {
        const string connectionString =
            "Host=localhost;Port=55432;Database=lawconnect;Username=lawconnect;Password=ChangeMe_StrongPassword!";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var probe = new ApplicationDbContext(options);
        if (!await probe.Database.CanConnectAsync())
        {
            Assert.Fail("PostgreSQL is not reachable at localhost:55432, so the booking lock was not verified.");
        }

        await using var firstDb = new ApplicationDbContext(options);
        await using var secondDb = new ApplicationDbContext(options);
        var first = new PostgresLawyerScheduleGuard(firstDb);
        var second = new PostgresLawyerScheduleGuard(secondDb);
        var lawyerId = Guid.NewGuid();
        var order = new List<string>();

        var held = first.RunExclusiveAsync(lawyerId, async ct =>
        {
            order.Add("start-1");
            await Task.Delay(300, ct);
            order.Add("end-1");
            return 1;
        });
        await Task.Delay(50);
        var waiting = second.RunExclusiveAsync(lawyerId, ct =>
        {
            order.Add("start-2");
            order.Add("end-2");
            return Task.FromResult(2);
        });

        await Task.WhenAll(held, waiting);
        Assert.Equal(["start-1", "end-1", "start-2", "end-2"], order);
    }
}
