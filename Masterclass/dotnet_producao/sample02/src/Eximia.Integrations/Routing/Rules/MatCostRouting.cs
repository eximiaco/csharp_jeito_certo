using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class MatCostRouting : RoutingRule
{
    private readonly IEnumerable<string> _eventValuationArea;

    public MatCostRouting(IEnumerable<string> eventValuationArea)
    {
        _eventValuationArea = eventValuationArea;
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

            var eventValuationArea = GetProperty(data, "EventValuationArea")?.ToString();
            if (!string.IsNullOrWhiteSpace(eventValuationArea) && _eventValuationArea.Contains(eventValuationArea))
                return ValueTask.FromResult(true);
            
            return ValueTask.FromResult(false);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing mat cost routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}
