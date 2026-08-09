using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Dispatches domain events after a successful save.
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatchInterceptor"/> class.
    /// </summary>
    /// <param name="scopeFactory">Scope factory used to resolve the dispatcher without circular DI.</param>
    public DomainEventDispatchInterceptor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchEventsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchEventsAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        if (entities.Count == 0)
        {
            return;
        }

        var domainEvents = entities
            .SelectMany(e =>
            {
                var events = e.DomainEvents.ToArray();
                e.ClearDomainEvents();
                return events;
            })
            .ToArray();

        if (domainEvents.Length == 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        await dispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}
