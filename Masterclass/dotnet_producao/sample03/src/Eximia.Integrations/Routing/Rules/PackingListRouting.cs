using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class PackingListRouting : RoutingRule
{
    private readonly IEnumerable<string> _ekorgs;

    public PackingListRouting(IEnumerable<string> ekorgs)
    {
        _ekorgs = ekorgs;
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

            var ekorg = GetProperty(data, "EKORG")?.ToString();
            if (string.IsNullOrWhiteSpace(ekorg) || !_ekorgs.Contains(ekorg))
                return ValueTask.FromResult(false);
            return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();
            logger.Error(ex, "Error processing packing list routing rule: {error}", ex.Message);
            return ValueTask.FromResult(false);
        }
    }
}