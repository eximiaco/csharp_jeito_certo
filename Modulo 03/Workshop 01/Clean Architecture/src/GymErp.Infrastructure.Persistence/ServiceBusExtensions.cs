using GymErp.Application.Abstractions;
using GymErp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace GymErp.Infrastructure.Persistence;

internal static class ServiceBusExtensions
{
    public static async Task DispatchDomainEventsAsync(this IEventPublisher eventPublisher, DbContext ctx, CancellationToken cancellationToken = default)
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
            await eventPublisher.PublishAsync(domainEvent, cancellationToken);
    }
}
