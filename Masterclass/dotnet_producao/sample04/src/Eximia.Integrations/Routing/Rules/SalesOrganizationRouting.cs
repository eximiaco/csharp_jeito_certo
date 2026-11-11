using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class SalesOrganizationRouting : RoutingRule
{
    private readonly IEnumerable<string> _salesOrganizationCodes;

    public SalesOrganizationRouting(IEnumerable<string> salesOrganizationCodes)
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

            var saleOrganization = GetProperty(data, "SalesOrganization")?.ToString();
            if (string.IsNullOrWhiteSpace(saleOrganization) || !_salesOrganizationCodes.Contains(saleOrganization))
                return ValueTask.FromResult(false);
            return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing sales organization routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
