using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class BatchRouting : RoutingRule
{
    private readonly IEnumerable<string> _plants;

    public BatchRouting(IEnumerable<string> plants)
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

            var batchPlant = GetProperty(data, "to_BatchPlant");
            if (batchPlant is null)
                return ValueTask.FromResult(false);
            
            var results = GetProperty(batchPlant, "results");
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
            logger.Error(ex, "Error processing batch routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
