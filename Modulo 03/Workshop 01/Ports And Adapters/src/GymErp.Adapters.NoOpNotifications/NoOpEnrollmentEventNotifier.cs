using GymErp.Core.Ports.Driven;

namespace GymErp.Adapters.NoOpNotifications;

// Adaptador conduzido que não avisa ninguém. Trocar por Kafka ou RabbitMQ não muda o núcleo.
public sealed class NoOpEnrollmentEventNotifier : IForNotifyingEnrollmentEvents
{
    public Task NotifyAsync(object domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
