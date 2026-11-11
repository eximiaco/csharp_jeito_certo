using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class PurchasingInfoRecordRouting : RoutingRule
{
    private readonly IEnumerable<string> _purchasingOrganizations;

    public PurchasingInfoRecordRouting(IEnumerable<string> purchasingOrganizations)
    {
        _purchasingOrganizations = purchasingOrganizations;
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

            var results = GetProperty(data, "results");
            if (results is not JsonArray resultsArray || resultsArray.Count == 0)
                return ValueTask.FromResult(false);

            foreach (var item in resultsArray)
            {
                if (item is not JsonObject itemObject)
                    continue;
                
                var purgInfo = GetProperty(itemObject, "to_PurgInfoRecdOrgPlantData");
                if (purgInfo is null)
                    continue;
                
                var purgInfoResults = GetProperty(purgInfo, "results");
                if (purgInfoResults is not JsonArray purgInfoResultsArray || purgInfoResultsArray.Count == 0)
                    continue;

                foreach (var purgInfoResult in purgInfoResultsArray)
                {
                    if (purgInfoResult is not JsonObject purgInfoResultObject)
                        continue;
                    
                    var purchasingOrganization = GetProperty(purgInfoResultObject, "PurchasingOrganization")?.ToString();
                    if (!string.IsNullOrWhiteSpace(purchasingOrganization) && _purchasingOrganizations.Contains(purchasingOrganization))
                        return ValueTask.FromResult(true);   
                }
            }
            
            return ValueTask.FromResult(false);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing purchasing info record routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
