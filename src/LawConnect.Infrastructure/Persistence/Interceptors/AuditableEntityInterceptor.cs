using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LawConnect.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Applies audit fields, soft-delete conversion, and concurrency version increments on save.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableEntityInterceptor"/> class.
    /// </summary>
    /// <param name="currentUser">Current user accessor.</param>
    /// <param name="dateTimeProvider">Clock abstraction.</param>
    public AuditableEntityInterceptor(
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
                entry.Entity.UpdatedBy = userId;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.RowVersion++;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = utcNow;
            entry.Entity.DeletedBy = userId;

            if (entry.Entity is BaseEntity baseEntity)
            {
                baseEntity.RowVersion++;
            }
        }
    }
}
