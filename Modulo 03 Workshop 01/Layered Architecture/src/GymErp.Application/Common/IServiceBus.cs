namespace GymErp.Application.Common;

public interface IServiceBus
{
    Task PublishAsync(object message, CancellationToken cancellationToken = default);
}
