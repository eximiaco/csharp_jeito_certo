using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.Routing.Rules;

public class DefaultRouting : RoutingRule
{
    public override ValueTask<bool> IsSatisfiedByAsync(RoutingRuleContext context, CancellationToken cancellationToken)
        => ValueTask.FromResult(true);
}