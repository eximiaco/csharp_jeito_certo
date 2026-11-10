using Silverback.Messaging.Broker.Behaviors;
using Eximia.Integrations.IntegrationEvents.Commands;

namespace Eximia.Integrations.HttpApi.Infrastructure.ProducerBehaviors;

public class AddDestinationHeaderProducerBehavior : IProducerBehavior
{
    public int SortIndex => 1999;

    public async ValueTask HandleAsync(
        ProducerPipelineContext context,
        ProducerBehaviorHandler next,
        CancellationToken cancellationToken)
    {
        var message = context.Envelope.Message as RouteIntegrationEventRequestCommand;
        if (message is not null)
        {
            context.Envelope.Headers["x-destiny"] = message!.Destiny;
            context.Envelope.Headers["x-controlCode"] = message!.ControlCode;
        }

        await next(context, cancellationToken);
    }
}
