using GymErp.Application.Common;

namespace GymErp.Infrastructure.ServiceBus;

public class NoOpServiceBus : IServiceBus
{
    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
