using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class FreightOrderRouting : RoutingRule
{
    private readonly IEnumerable<string> _purchCompanyCodes;

    public FreightOrderRouting(IEnumerable<string> purchCompanyCodes)
    {
        _purchCompanyCodes = purchCompanyCodes;
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

            var empresaParticipantes = GetProperty(data, "to_EmpresasParticipantes");
            if (empresaParticipantes is null)
                return ValueTask.FromResult(false);
            
            var results = GetProperty(empresaParticipantes, "results");
            if (results is not JsonArray resultsArray || resultsArray.Count == 0)
                return ValueTask.FromResult(false);

            foreach (var item in resultsArray)
            {
                if (item is not JsonObject itemObject)
                    continue;
                
                var purchCompanyCode = GetProperty(itemObject, "PurchCompanyCode")?.ToString();
                if (!string.IsNullOrWhiteSpace(purchCompanyCode) && _purchCompanyCodes.Contains(purchCompanyCode))
                    return ValueTask.FromResult(true);        
            }
            
            return ValueTask.FromResult(false);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing freight order routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
