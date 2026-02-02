using Autofac;

namespace GymErp.Common.Infrastructure;

public class CommonModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // IServiceBus is registered by AddRabbitMq (RabbitMqServiceBus).
    }
}