namespace GymErp.Application.Ports.Outbound;

public interface IEventPublisher
{
    Task PublishAsync(object message, CancellationToken cancellationToken = default);
}
