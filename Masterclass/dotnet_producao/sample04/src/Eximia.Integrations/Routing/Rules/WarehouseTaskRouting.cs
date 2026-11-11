using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class WarehouseTaskRouting : RoutingRule
{
    private readonly IEnumerable<string> _types;
    private readonly IEnumerable<string> _whses;
    
    public WarehouseTaskRouting(IEnumerable<string> types, IEnumerable<string> whses)
    {
        _types = types;
        _whses = whses;
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

            var warehouse = GetProperty(data, "whse")?.ToString();
            var type = GetProperty(data, "type")?.ToString();
            if (string.IsNullOrWhiteSpace(warehouse) || string.IsNullOrWhiteSpace(type))
                return ValueTask.FromResult(false);
            return ValueTask.FromResult(_types.Contains(type) && _whses.Contains(warehouse));
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing warehouse task routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}