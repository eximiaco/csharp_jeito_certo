using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class ResultsCompanyCodeRouting : RoutingRule
{
    private readonly IEnumerable<string> _companyCodes;

    public ResultsCompanyCodeRouting(IEnumerable<string> companyCodes)
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

            var results = GetProperty(data, "results");
            if (results is not JsonArray resultsArray || resultsArray.Count == 0)
                return ValueTask.FromResult(false);

            foreach (var item in resultsArray)
            {
                if (item is not JsonObject itemObject)
                    continue;
                
                var companyCode = GetProperty(itemObject, "CompanyCode")?.ToString();
                if (!string.IsNullOrWhiteSpace(companyCode) && _companyCodes.Contains(companyCode))
                    return ValueTask.FromResult(true);   
            }
            
            return ValueTask.FromResult(false);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing results company code routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
