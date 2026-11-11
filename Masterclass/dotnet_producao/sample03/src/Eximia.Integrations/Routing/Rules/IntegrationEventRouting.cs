using Microsoft.Extensions.DependencyInjection;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class IntegrationEventRouting : RoutingRule
{
    public override async ValueTask<bool> IsSatisfiedByAsync(RoutingRuleContext context, CancellationToken cancellationToken)
    {
        if (!context.IntegrationEventId.HasValue)
            return false;
        
        var integrationEventsDataAccess = context.ServiceProvider.GetRequiredService<IntegrationEventsDataAccess>();
        var destiny = await integrationEventsDataAccess.GetDestinyAsync(context.IntegrationEventId.Value, cancellationToken);
        if (destiny.HasNoValue)
            return false;
        return destiny.Value.Equals(context.Route.Destination, StringComparison.InvariantCultureIgnoreCase);
    }
}