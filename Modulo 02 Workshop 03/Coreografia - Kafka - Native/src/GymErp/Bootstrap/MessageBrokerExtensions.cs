using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Bootstrap;

internal static class MessageBrokerExtensions
{
    /// <summary>
    /// Configures Kafka using Confluent.Kafka (confluent-kafka-dotnet).
    /// </summary>
    public static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddKafka(configuration);
    }
}
