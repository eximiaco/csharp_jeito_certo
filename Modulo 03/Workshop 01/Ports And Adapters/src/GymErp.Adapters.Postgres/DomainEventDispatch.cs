using GymErp.Core.Ports.Driven;
using GymErp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace GymErp.Adapters.Postgres;

internal static class DomainEventDispatch
{
    public static async Task DispatchDomainEventsAsync(this IForNotifyingEnrollmentEvents notifier, DbContext ctx, CancellationToken cancellationToken = default)
    {
        var domainEntities = ctx.ChangeTracker
            .Entries<Aggregate>()
            .Where(x => x.Entity.DomainEvents.Count != 0);

        var entityEntries = domainEntities.ToList();

        var domainEvents = entityEntries
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();
        entityEntries.ForEach(entity => entity.Entity.ClearDomainEvents());
        foreach (var domainEvent in domainEvents)
            await notifier.NotifyAsync(domainEvent, cancellationToken);
    }
}
