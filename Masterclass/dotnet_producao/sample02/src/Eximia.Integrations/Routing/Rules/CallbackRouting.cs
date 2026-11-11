using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class CallbackRouting : RoutingRule
{
    public override ValueTask<bool> IsSatisfiedByAsync(RoutingRuleContext context, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = GetProperty(context.Payload, "metadata");
            if (metadata is null)
                return ValueTask.FromResult(false);
            
            var isCallback = GetProperty(metadata, "isCallback")?.ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false;
            if (!isCallback)
                return ValueTask.FromResult(false);
            
            var destiny = GetProperty(metadata, "destiny");
            if (destiny is null)
                return ValueTask.FromResult(false);
            
            var destinyName = GetProperty(destiny, "name")?.ToString();
            if (string.IsNullOrEmpty(destinyName) || !destinyName.Equals(context.Route.Destination, StringComparison.InvariantCultureIgnoreCase))
                return ValueTask.FromResult(false);
            
            return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing callback routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
