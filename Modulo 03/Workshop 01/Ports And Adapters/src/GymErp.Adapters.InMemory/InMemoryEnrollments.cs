using System.Collections.Concurrent;
using GymErp.Core.Ports.Driven;
using GymErp.Domain.Enrollments;

namespace GymErp.Adapters.InMemory;

// Cockburn: a aplicação deve poder ser "developed and tested in isolation from its eventual run-time devices and databases".
public sealed class InMemoryEnrollments(IForNotifyingEnrollmentEvents notifier) : IForStoringEnrollments
{
    private readonly ConcurrentDictionary<Guid, Enrollment> _items = new();

    public Task<Enrollment?> FindAsync(Guid enrollmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(enrollmentId));

    public async Task SaveAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        _items[enrollment.Id] = enrollment;

        // Mesmo contrato do adaptador Postgres: depois de guardar, avisa os eventos pendentes.
        var pending = enrollment.DomainEvents.ToList();
        enrollment.ClearDomainEvents();
        foreach (var domainEvent in pending)
            await notifier.NotifyAsync(domainEvent, cancellationToken);
    }
}
