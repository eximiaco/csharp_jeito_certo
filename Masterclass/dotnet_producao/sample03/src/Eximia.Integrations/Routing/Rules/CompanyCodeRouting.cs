using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class CompanyCodeRouting : RoutingRule
{
    private readonly IEnumerable<string> _companyCodes;
    
    public CompanyCodeRouting(IEnumerable<string> companyCodes)
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

            var companyCode = GetProperty(data, "CompanyCode")?.ToString();
            if (string.IsNullOrWhiteSpace(companyCode) || !_companyCodes.Contains(companyCode))
                return ValueTask.FromResult(false);
            return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing company code routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
