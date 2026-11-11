using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class FciRouting : RoutingRule
{
    private readonly IEnumerable<string> _companyCodes;

    public FciRouting(IEnumerable<string> companyCodes)
    {
        _companyCodes = companyCodes;
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

            var fciObject = GetProperty(data, "ZTLB_FINISH_PROD");
            if (fciObject is not JsonArray fciArray || fciArray.Count == 0)
                return ValueTask.FromResult(false);

            foreach (var fciItem in fciArray)
            {
                if (fciItem is not JsonObject fciObjectItem)
                    continue;
                
                var companyCode = GetProperty(fciObjectItem, "empresa")?.ToString();
                if (!string.IsNullOrWhiteSpace(companyCode) && _companyCodes.Contains(companyCode))
                    return ValueTask.FromResult(true);        
            }
            
            return ValueTask.FromResult(false);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing FCI routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
