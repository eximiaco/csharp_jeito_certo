using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Eximia.Integrations.IntegrationEvents.Commands;

namespace Eximia.Integrations.IntegrationEvents.Application.Routing;

public class RouteIntegrationEventConsumer(RouteIntegrationEventApplication routeIntegrationEventApplication)
{
    public Task ConsumeMessage(IInboundEnvelope<RouteIntegrationEventRequestCommand> message, CancellationToken cancellationToken)
        => routeIntegrationEventApplication.RouteAsync(
            message.GetKafkaOffset(),
            message.Message!,
            cancellationToken);
}
