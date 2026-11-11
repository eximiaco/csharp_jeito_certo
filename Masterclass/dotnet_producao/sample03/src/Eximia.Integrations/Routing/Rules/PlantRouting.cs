using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class PlantRouting : RoutingRule
{
    private readonly IEnumerable<string> _plants;
    
    public PlantRouting(IEnumerable<string> plants)
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

            var plant = GetProperty(data, "Plant")?.ToString();
            if (plant is null)
                return ValueTask.FromResult(false);
            
            if (string.IsNullOrWhiteSpace(plant) || !_plants.Contains(plant))
                return ValueTask.FromResult(false);
           return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing plant routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
