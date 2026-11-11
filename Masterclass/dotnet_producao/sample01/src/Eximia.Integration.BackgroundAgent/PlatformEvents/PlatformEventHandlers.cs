using System.Text.Json;
using Eximia.Integrations.PlatformEvents;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.PlatformEvents;

/// <summary>
///     Reage a um evento da plataforma. Os handlers rodam num escopo por mensagem e podem ser executados mais de uma
///     vez para a mesma mensagem (o consumer é at-least-once): precisam ser IDEMPOTENTES. São síncronos de propósito:
///     rodam na thread dedicada do consumer (ver <c>PlatformEventsConsumer</c>).
/// </summary>
public interface IPlatformEventHandler
{
    void Handle(PlatformEventContext context);
}

/// <summary>Grava o evento na linha do tempo da integração.</summary>
public sealed class TimelineHandler(PlatformEventTimelineDataAccess dataAccess) : IPlatformEventHandler
{
    public void Handle(PlatformEventContext context) => dataAccess.Record(context);
}

/// <summary>Registra um alerta quando uma integração falha. É o ponto onde entraria um aviso a quem opera o sistema.</summary>
public sealed class ErrorAlertHandler(ILogger logger) : IPlatformEventHandler
{
    public void Handle(PlatformEventContext context)
    {
        if (context.Event.Name != Eximia.Integrations.Events.PlataformEventType.IntegrationEventError)
            return;

        logger.Warning("Integration {ControlCode} failed: {Error}",
            context.Event.ControlCode, ReadError(context.Event.Data));
    }

    private static string? ReadError(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in data.EnumerateObject())
            if (property.Name.Equals("ErrorMessage", StringComparison.OrdinalIgnoreCase))
                return property.Value.GetString();

        return null;
    }
}
