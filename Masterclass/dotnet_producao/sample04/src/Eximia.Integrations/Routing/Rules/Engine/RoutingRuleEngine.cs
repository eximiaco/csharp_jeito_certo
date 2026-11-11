using System.Text.Json.Nodes;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.Routing.Rules.Engine;

public class RoutingRuleEngine(IServiceProvider serviceProvider)
{
    public async Task<IEnumerable<Route>> FilterApplicableRoutesAsync(
        IEnumerable<Route> routes,
        JsonNode payload,
        int? integrationEventId,
        CancellationToken cancellationToken)
    {
        var applicableRoutes = new List<Route>();
        
        foreach (var route in routes)
        {
            RoutingRuleContext context = new(serviceProvider, payload, route, integrationEventId);
            
            foreach (var rule in route.Rules)
            {
                if (!await rule.IsSatisfiedByAsync(context, cancellationToken))
                    continue;
                
                applicableRoutes.Add(route);
                break;
            }
        }

        return applicableRoutes;
    }
}
