using GymErp.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Bootstrap;

internal static class MessageBrokerExtensions
{
    /// <summary>
    /// Configures the message broker based on appsettings (key "MessageBroker": "Silverback" or "MassTransit").
    /// Each broker extension registers itself in MessageBrokerRegistry; this method invokes the selected one.
    /// </summary>
    public static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        // Ensure both broker extensions are loaded so their static constructors run and they register in the registry.
        _ = typeof(SilverbackServiceExtensions);
        _ = typeof(MassTransitServiceExtensions);

        var brokerName = configuration["MessageBroker"] ?? "Silverback";
        MessageBrokerRegistry.Invoke(brokerName, services, configuration);
        return services;
    }
}
