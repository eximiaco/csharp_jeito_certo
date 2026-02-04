namespace GymErp.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(object message, CancellationToken cancellationToken = default);
}
