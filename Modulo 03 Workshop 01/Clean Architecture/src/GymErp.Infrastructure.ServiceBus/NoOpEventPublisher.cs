using GymErp.Application.Abstractions;

namespace GymErp.Infrastructure.ServiceBus;

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
