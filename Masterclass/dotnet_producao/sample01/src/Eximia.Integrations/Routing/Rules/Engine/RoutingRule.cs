using System.Text.Json.Nodes;

namespace Eximia.Integrations.Routing.Rules.Engine;

public abstract class RoutingRule
{
    public abstract ValueTask<bool> IsSatisfiedByAsync(RoutingRuleContext context, CancellationToken cancellationToken);

    protected bool IsValid(RoutingRuleContext context)
    {
        var metadata = GetProperty(context.Payload, "metadata");
        if (metadata is null)
            return false;

        var isCallback = GetProperty(metadata, "isCallback")?.ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false;
        if (isCallback)
            return false;

        var entity = GetProperty(metadata, "entity");
        if (entity is null)
            return false;

        var entityName = GetProperty(entity, "name")?.ToString();
        return !string.IsNullOrWhiteSpace(entityName) &&
               context.Route.Entity.Equals(entityName, StringComparison.InvariantCultureIgnoreCase);
    }

    protected static JsonNode? GetProperty(JsonNode? node, string propertyName)
    {
        if (node is not JsonObject jsonObject)
            return null;

        var match = jsonObject.FirstOrDefault(p
            => p.Key.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));

        return match.Value;
    }
}
