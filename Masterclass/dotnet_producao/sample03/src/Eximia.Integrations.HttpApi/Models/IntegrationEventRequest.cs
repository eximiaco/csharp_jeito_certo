using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Eximia.Integrations.IntegrationEvents;

namespace Eximia.Integrations.HttpApi.Models;

public record IntegrationEventRequest([Required]Metadata Meta, JsonNode Payload);

public record Metadata(
    [Required] Source Origin,
    [Required] Source Destiny,
    [Required] Entity Entity,
    string CorrelationKey,
    int? SchemaVersion,
    string? SapQueryAction,
    int? StatusCode,
    bool IsCallbackError)
{
    public int? IntegrationEventId { get; set; }
};

public record Entity(string Name, long Version, string Action, string? AggregateKey);