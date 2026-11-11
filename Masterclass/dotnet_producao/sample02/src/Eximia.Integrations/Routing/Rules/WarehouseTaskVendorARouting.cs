using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class WarehouseTaskVendorARouting : RoutingRule
{
    private readonly IEnumerable<string> _warehouses;

    public WarehouseTaskVendorARouting(IEnumerable<string> warehouses)
    {
        _warehouses = warehouses;
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
            if (string.IsNullOrWhiteSpace(warehouse) || !_warehouses.Contains(warehouse))
                return ValueTask.FromResult(false);
            return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing warehouse task vendor A routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}