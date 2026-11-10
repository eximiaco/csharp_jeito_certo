using CSharpFunctionalExtensions;
using Serilog;
using Serilog.Context;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Outbox;
using Eximia.Integrations.Routing;

namespace Eximia.Integrations.IntegrationEvents.Telemetry;

public sealed class RegisterNewIntegrationEventOtelTelemetry(
    ITelemetryFactory telemetryFactory,
    PlatformMetrics platformMetrics,
    OtelVariables otelVariables,
    OutboxMetrics outboxMetrics,
    SquadsSingleton squadsSingleton,
    ILogger logger) : IRegisterNewIntegrationEventTelemetry, IDisposable
{
    private IOtelTelemetry? _activity = telemetryFactory.Create("RegisterNewIntegrationEventApplication.Handle");
    private readonly SquadsSingleton _squadsSingleton = squadsSingleton;
    private bool _disposed = false;

    public void IntegrationEventReceived(RegisterAsyncIntegrationEventCommand command)
    {
        var squad = _squadsSingleton.GetByEntity(command.Entity);

        _activity!
            .AddTag(otelVariables.Destiny, command.Destiny)
            .AddTag(otelVariables.Origin, command.Origin)
            .AddTag(otelVariables.Entity, command.Entity)
            .AddTag(otelVariables.AggregateKey, command.AggregateKey)
            .AddTag(otelVariables.ControlCode, command.CorrelationId)
            .AddTag(otelVariables.Squad, squad)
            .AddTag(otelVariables.IsCallback, command.IsCallback);

        logger.Information("New integration request received.");
    }

    public ResultAggregate RouteNotFound(RegisterAsyncIntegrationEventCommand command)
    {
        _activity!.SetError("No route for destiny and entity");        
        logger.Error("No route found.");

        Dispose(true);
        return ResultAggregate.Failure("Destiny", "No route for destiny and entity");
    }

    public void RouteFounded(Route route)
    {
        _activity!
            .AddTag(otelVariables.RouteId, route.Id)
            .AddEvent("Route founded");

        logger.Debug("Route founded.");
    }

    public ResultAggregate SchemaNotFound(RegisterAsyncIntegrationEventCommand command)
    {
        _activity!.SetError("Schema not founded");
        Dispose(true);
        return ResultAggregate.Failure("Schema", "Schema is invalid");
    }

    public ResultAggregate FailOnCreateIntegrationEvent(RegisterAsyncIntegrationEventCommand command, Dictionary<string, Result> errors)
    {
        var parsedErrors = errors.ToJson();

        _activity!
            .AddTag(otelVariables.Error, parsedErrors)
            .SetError("Failed to create integration event");

        using (LogContext.PushProperty(otelVariables.Error, parsedErrors))
            logger.Error("Failed to create integration event.");

        Dispose(true);
        return ResultAggregate.Failure(errors);
    }

    public ResultAggregate FailOnPolicyRules(RegisterAsyncIntegrationEventCommand command, Dictionary<string, Result> errors)
    {
        var parsedErrors = errors.ToJson();

        _activity!
            .AddTag(otelVariables.Error, parsedErrors)
            .SetError("Failed to execute policies");

        using (LogContext.PushProperty(otelVariables.Error, parsedErrors))
            logger.Error("Failed to execute policies.");

        Dispose(true);
        return ResultAggregate.Failure(errors);
    }

    public void IntegrationEventCreated(IntegrationEvent integrationEvent)
    {
        _activity!
            .AddTag(otelVariables.IntegrationEventId, integrationEvent.Id)
            .AddTag(otelVariables.ControlCode, integrationEvent.ControlCode)
            .AddEvent("Integration event created");

        var squad = _squadsSingleton.GetByEntity(integrationEvent.Entity.Entity);

        platformMetrics.IntegrationAsyncRequestCreated(
            integrationEvent.Entity.Origin.Name,
            integrationEvent.Entity.Destiny.Name,
            integrationEvent.Entity.Entity,
            squad,
            integrationEvent.Control.IsCallback);

        logger.Information("Integration event created.");
    }

    public void IntegrationEventPublish(IntegrationEvent integrationEvent)
    {
        _activity!.AddEvent("Integration event published");

        var squad = _squadsSingleton.GetByEntity(integrationEvent.Entity.Entity);

        outboxMetrics.EnqueueMessage(
            integrationEvent.Entity.Origin.Name!,
            integrationEvent.Entity.Destiny.Name!,
            integrationEvent.Entity.Entity,
            squad,
            integrationEvent.Control.IsCallback);
        
        logger.Information("Integration event published.");
    }

    public void FatalErrorCreatingIntegrationEvent(Exception exception)
    {
        _activity?
            .AddExceptionAndEvent(exception.Message, exception)
            .SetError(exception.Message);
        
        logger.Fatal(exception, exception.Message);
        Dispose(true);
    }

    public void IntegrationEventRequestProcessed()
    {
        _activity!.SetSuccess("Integration event processed");
        Dispose(true);
    }

    public void Dispose()
    {
        Dispose(true);
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed) 
            return;
        if (disposing)
        {
            _activity!.Dispose();
            _activity = null;
        }
        _disposed = true;
    }
}
