using System.Text.Json.Nodes;

namespace Eximia.Integrations.Routing.Rules.Engine;

public record RoutingRuleContext(IServiceProvider ServiceProvider, JsonNode Payload, Route Route, int? IntegrationEventId);