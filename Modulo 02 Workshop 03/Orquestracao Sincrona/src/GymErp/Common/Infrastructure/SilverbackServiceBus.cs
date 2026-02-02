using Silverback.Messaging.Publishing;

namespace GymErp.Common.Infrastructure;

public class SilverbackServiceBus(IPublisher publisher) : IServiceBus
{
    public async Task PublishAsync(object message)
    {
        await publisher.PublishAsync(message);
    }
}