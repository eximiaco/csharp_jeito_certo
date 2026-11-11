using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class SalesOrganizationDeleteRouting : RoutingRule
{
    private readonly IEnumerable<string> _salesOrganizationCodes;

    public SalesOrganizationDeleteRouting(IEnumerable<string> salesOrganizationCodes)
    {
        _salesOrganizationCodes = salesOrganizationCodes;
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
            
            var secondData = GetProperty(data, "data");
            if (secondData is null)
                return ValueTask.FromResult(false);
            
            var saleOrganization = GetProperty(secondData, "SalesOrganization")?.ToString();
            if (string.IsNullOrWhiteSpace(saleOrganization) || !_salesOrganizationCodes.Contains(saleOrganization))
                return ValueTask.FromResult(false);
            return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing sales organization delete routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
