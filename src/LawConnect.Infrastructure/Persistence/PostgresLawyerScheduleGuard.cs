using LawConnect.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Infrastructure.Persistence;

/// <summary>
/// <see cref="ILawyerScheduleGuard"/> backed by a PostgreSQL transaction-scoped advisory lock,
/// so it holds across every API instance sharing the database.
/// </summary>
public sealed class PostgresLawyerScheduleGuard : ILawyerScheduleGuard
{
    /// <summary>First key of the two-key advisory lock; namespaces these locks away from any others.</summary>
    private const int LockNamespace = 0x4C415750; // "LAWP"

    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresLawyerScheduleGuard"/> class.
    /// </summary>
    public PostgresLawyerScheduleGuard(ApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<T> RunExclusiveAsync<T>(
        Guid lawyerId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (!_dbContext.Database.IsNpgsql())
        {
            return await operation(cancellationToken);
        }

        var lockKey = ToLockKey(lawyerId);
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0}, {1})",
                [LockNamespace, lockKey],
                ct);

            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }, cancellationToken);
    }

    private static int ToLockKey(Guid lawyerId)
    {
        Span<byte> bytes = stackalloc byte[16];
        lawyerId.TryWriteBytes(bytes);
        return BitConverter.ToInt32(bytes[..4]) ^ BitConverter.ToInt32(bytes[4..8])
            ^ BitConverter.ToInt32(bytes[8..12]) ^ BitConverter.ToInt32(bytes[12..16]);
    }
}
