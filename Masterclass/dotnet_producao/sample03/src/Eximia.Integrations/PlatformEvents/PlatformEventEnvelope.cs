using System.Text.Json;

namespace Eximia.Integrations.PlatformEvents;

/// <summary>
///     O evento da plataforma como ele trafega no tópico (<see cref="Events.PlataformEvent" /> serializado em JSON).
///     O consumer não depende do tipo do produtor: lê só o envelope e deixa o <see cref="Data" /> como JSON.
/// </summary>
public sealed record PlatformEventEnvelope(string Name, Guid ControlCode, DateTime Timestamp, JsonElement Data)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <exception cref="JsonException">A mensagem não é um evento válido (mensagem venenosa).</exception>
    public static PlatformEventEnvelope Parse(string json)
        => JsonSerializer.Deserialize<PlatformEventEnvelope>(json, Options)
           ?? throw new JsonException("Mensagem vazia.");
}

/// <summary>De onde o evento veio: topic/partition/offset identificam a mensagem de forma única.</summary>
public sealed record PlatformEventContext(PlatformEventEnvelope Event, string Topic, int Partition, long Offset);
