namespace GymErp.Common;

public interface IServiceBus
{
    Task PublishAsync(object message);
}