namespace GymErp.Bootstrap;

internal static class MessageBrokerExtensions
{
    /// <summary>
    /// Configures RabbitMQ using the official RabbitMQ.Client (rabbitmq-dotnet-client).
    /// </summary>
    public static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddRabbitMq(configuration);
    }
}
