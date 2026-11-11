using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class InspectionLotRouting : RoutingRule
{
    private readonly IEnumerable<string> _plants;

    public InspectionLotRouting(IEnumerable<string> plants)
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

            var inspectionLotType = GetProperty(data, "A_InspectionLotType");
            if (inspectionLotType is null)
                return ValueTask.FromResult(false);
            
            var plant = GetProperty(inspectionLotType, "Plant")?.ToString();
            return ValueTask.FromResult(!string.IsNullOrWhiteSpace(plant) && _plants.Contains(plant));
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing inspection lot routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
