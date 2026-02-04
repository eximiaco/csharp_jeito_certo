using GymErp.Application.Common;
using GymErp.CrossCutting;
using Microsoft.EntityFrameworkCore;

namespace GymErp.Infrastructure.Persistence;

internal static class ServiceBusExtensions
{
    public static async Task DispatchDomainEventsAsync(this IServiceBus serviceBus, DbContext ctx, CancellationToken cancellationToken = default)
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
            await serviceBus.PublishAsync(domainEvent, cancellationToken);
    }
}
