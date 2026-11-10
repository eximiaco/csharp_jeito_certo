using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.HttpApi.Infrastructure;
using Eximia.Integrations.HttpApi.Models;
using Eximia.Integrations.IntegrationEvents.Application.Register;
using Eximia.Integrations.IntegrationEvents.Commands;

namespace Eximia.Integrations.HttpApi.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class IntegrationEventsController(
    EximiaProblemDetailsFactory detailsFactory,
    OtelVariables otelVariables,
    SquadsSingleton squadsSingleton,
    IHttpContextAccessor httpContextAccessor)
    : ControllerBase
{
    [HttpPost("integrations")]
    [Consumes("application/json")]
    [RequestSizeLimit(100_000_000)]
    public Task<IActionResult> RegisterNewEvent(
        [FromServices] RegisterMultipleRoutesIntegrationEventApplication application,
        [FromServices] ITelemetryFactory telemetryFactory,
        [FromBody] IntegrationEventRequest payload,
        CancellationToken cancellationToken)
    {
        return ProcessNewIntegrationEventRequest(application, telemetryFactory, payload, false, cancellationToken);
    }

    [HttpPost("integrations/callback")]
    [Consumes("application/json")]
    [RequestSizeLimit(100_000_000)]
    public Task<IActionResult> RegisterNewCallbackEvent(
        [FromServices] RegisterMultipleRoutesIntegrationEventApplication application,
        [FromServices] ITelemetryFactory telemetryFactory,
        [FromBody] IntegrationEventRequest payload,
        CancellationToken cancellationToken)
    {
        return ProcessNewIntegrationEventRequest(application, telemetryFactory, payload, true, cancellationToken);
    }

    private async Task<IActionResult> ProcessNewIntegrationEventRequest(
        RegisterMultipleRoutesIntegrationEventApplication application,
        ITelemetryFactory telemetryFactory,
        IntegrationEventRequest payload,
        bool isCallback,
        CancellationToken cancellationToken)
    {
        var activity = telemetryFactory.GetFromActivity(HttpContext.Features.Get<IHttpActivityFeature>()?.Activity!);

        using var destinyLog = LogContext.PushProperty(otelVariables.Destiny, payload.Meta.Destiny.Name);
        using var destinyKeyLog = LogContext.PushProperty(otelVariables.DestinyKey, payload.Meta.Destiny.Key);
        using var originLog = LogContext.PushProperty(otelVariables.Origin, payload.Meta.Origin.Name);
        using var originKeyLog = LogContext.PushProperty(otelVariables.Origin, payload.Meta.Origin.Key);
        using var entityLog = LogContext.PushProperty(otelVariables.Entity, payload.Meta.Entity.Name);
        using var squadLog = LogContext.PushProperty(otelVariables.Squad, squadsSingleton.GetByEntity(payload.Meta.Entity.Name));
        using var aggregateKeyLog = LogContext.PushProperty(otelVariables.AggregateKey, payload.Meta.Entity.AggregateKey);
        using var isCallbackLog = LogContext.PushProperty(otelVariables.IsCallback, isCallback);
        using var isCallbackErrorLog = LogContext.PushProperty(otelVariables.IsCallbackError, payload.Meta.IsCallbackError);

        var isPayloadLink = false;
        if (httpContextAccessor.HttpContext?.Request?.Headers?.TryGetValue("x-eximia-is-payload-link", out var isPayloadLinkHeader) == true)
            isPayloadLink = isPayloadLinkHeader.ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase);

        var payloadFile = string.Empty;
        if (httpContextAccessor.HttpContext?.Request?.Headers?.TryGetValue("x-eximia-payload-file", out var payloadFileHeader) == true)
            payloadFile = payloadFileHeader;

        var fileId = Guid.Empty;
        if (httpContextAccessor.HttpContext?.Request?.Headers?.TryGetValue("x-eximia-payload-file-id", out var payloadFileIdHeader) == true)
            Guid.TryParse(payloadFileIdHeader, out fileId);

        var command = RegisterMultipleRoutesIntegrationEventCommand.Create(
            payload.Meta.Origin.Name ?? string.Empty,
            payload.Meta.Origin.Key ?? string.Empty,
            payload.Meta.Destiny.Name ?? string.Empty,
            payload.Meta.Destiny.Key ?? string.Empty,
            payload.Meta.Entity.Name,
            payload.Meta.Entity.Action,
            payload.Meta.CorrelationKey,
            payload.Payload,
            payload.Meta.Entity.AggregateKey ?? string.Empty,
            payload.Meta.SchemaVersion ?? 0,
            isCallback,
            payload.Meta.IsCallbackError,
            isPayloadLink,
            payloadFile ?? string.Empty,
            fileId,
            payload.Meta.SapQueryAction,
            payload.Meta.StatusCode,
            payload.Meta.Entity.Version,
            payload.Meta.IntegrationEventId);
        if (command.IsFailure)
        {
            activity.SetError("Request has invalid parameters");
            return detailsFactory.Create400WithMessages(
                "Request invalid",
                "Request has invalid params",
                command);
        }

        var result = await application.HandleV3(command.Value, cancellationToken);
        if (result.IsFailure)
        {
            activity.SetError("Request processing error");
            return detailsFactory.Create400WithMessages(
                "Request error",
                "Request processing error",
                result);
        }

        activity.SetSuccess("Success");
        return Accepted();
    }
}
