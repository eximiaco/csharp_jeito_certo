using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class InboundDeliveryRouting : RoutingRule
{
    private readonly IEnumerable<string> _plants;

    public InboundDeliveryRouting(IEnumerable<string> plants)
    {
        _plants = plants;
    }

    public override ValueTask<bool> IsSatisfiedByAsync(RoutingRuleContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsValid(context))
                return ValueTask.FromResult(false);
            
            var data = GetProperty(context.Payload, "data");
            if (data is null)
                return ValueTask.FromResult(false);

            var deliveryDocumentItem = GetProperty(data, "to_DeliveryDocumentItem");
            if (deliveryDocumentItem is null)
                return ValueTask.FromResult(false);
            
            var results = GetProperty(deliveryDocumentItem, "results");
            if (results is not JsonArray resultsArray || resultsArray.Count == 0)
                return ValueTask.FromResult(false);

            foreach (var item in resultsArray)
            {
                if (item is not JsonObject itemObject)
                    continue;
                
                var plant = GetProperty(itemObject, "Plant")?.ToString();
                if (!string.IsNullOrWhiteSpace(plant) && _plants.Contains(plant))
                    return ValueTask.FromResult(true);
            }
            
            return ValueTask.FromResult(false);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing inbound delivery routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
