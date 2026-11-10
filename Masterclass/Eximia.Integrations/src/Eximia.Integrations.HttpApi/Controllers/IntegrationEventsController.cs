using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Eximia.Integrations.HttpApi.Infrastructure;
using Eximia.Integrations.HttpApi.Models;
using Eximia.Integrations.IntegrationEvents.Application.Register;
using Eximia.Integrations.IntegrationEvents.Commands;

namespace Eximia.Integrations.HttpApi.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class IntegrationEventsController(
    EximiaProblemDetailsFactory detailsFactory)
    : ControllerBase
{
    [HttpPost("integrations")]
    [Consumes("application/json")]
    [RequestSizeLimit(100_000_000)]
    public Task<IActionResult> RegisterNewEvent(
        [FromServices] RegisterMultipleRoutesIntegrationEventApplication application,
        [FromBody] IntegrationEventRequest payload,
        CancellationToken cancellationToken)
    {
        return ProcessNewIntegrationEventRequest(application, payload, false, cancellationToken);
    }

    [HttpPost("integrations/callback")]
    [Consumes("application/json")]
    [RequestSizeLimit(100_000_000)]
    public Task<IActionResult> RegisterNewCallbackEvent(
        [FromServices] RegisterMultipleRoutesIntegrationEventApplication application,
        [FromBody] IntegrationEventRequest payload,
        CancellationToken cancellationToken)
    {
        return ProcessNewIntegrationEventRequest(application, payload, true, cancellationToken);
    }

    private async Task<IActionResult> ProcessNewIntegrationEventRequest(
        RegisterMultipleRoutesIntegrationEventApplication application,
        IntegrationEventRequest payload,
        bool isCallback,
        CancellationToken cancellationToken)
    {
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
            payload.Meta.SapQueryAction,
            payload.Meta.StatusCode,
            payload.Meta.Entity.Version,
            payload.Meta.IntegrationEventId);
        if (command.IsFailure)
        {
            return detailsFactory.Create400WithMessages(
                "Request invalid",
                "Request has invalid params",
                command);
        }

        var result = await application.HandleV3(command.Value, cancellationToken);
        if (result.IsFailure)
        {
            return detailsFactory.Create400WithMessages(
                "Request error",
                "Request processing error",
                result);
        }

        return Accepted();
    }
}
