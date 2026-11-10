namespace GymErp.Core.Ports.Driven;

// Porta conduzida para avisar o mundo sobre o que aconteceu com a matrícula.
// Hoje um no-op; amanhã Kafka, RabbitMQ ou e-mail, sem mudar o núcleo.
public interface IForNotifyingEnrollmentEvents
{
    Task NotifyAsync(object domainEvent, CancellationToken cancellationToken = default);
}
