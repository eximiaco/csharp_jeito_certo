using Confluent.Kafka;
using Serilog;
using Silverback.Messaging.Broker;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.Shared.Telemetry;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Eximia.Integrations.IntegrationEvents.Telemetry;

public sealed class RouteIntegrationEventOtelTelemetry(
    ITelemetryFactory telemetryFactory,
    PlatformMetrics platformMetrics,
    OtelVariables otelVariables,
    ILogger logger,
    SquadsSingleton squadsSingleton) : IRouteIntegrationEventTelemetry
{
    private IOtelTelemetry? _activity = telemetryFactory.Create("RouteIntegrationEventApplication.ConsumeMessage");
    private readonly SquadsSingleton _squadsSingleton = squadsSingleton;

    public void IntegrationEventConsumed(RouteIntegrationEventRequestCommand command, KafkaOffset kafkaMessage)
    {
        var squad = _squadsSingleton.GetByEntity(command.Entity);

        _activity?
            .AddTag(otelVariables.KafkaTopicName, kafkaMessage.TopicPartition.Topic)
            .AddTag(otelVariables.KafkaTopicOffset, $"[{kafkaMessage.TopicPartition.Partition.Value}]@{kafkaMessage.Offset.Value}")
            .AddTag(otelVariables.KafkaMessageKey, $"{kafkaMessage.TopicPartition.Topic}[{kafkaMessage.TopicPartition.Partition.Value}]")
            .AddTag(otelVariables.IntegrationEventId, command.Id)
            .AddTag(otelVariables.ControlCode, command.ControlCode)
            .AddTag(otelVariables.AggregateKey, command.Key)
            .AddTag(otelVariables.KafkaTopicName, command.Topic)
            .AddTag(otelVariables.Origin, command.Origin)
            .AddTag(otelVariables.Destiny, command.Destiny)
            .AddTag(otelVariables.Entity, command.Entity)
            .AddTag(otelVariables.Squad, squad)
            .AddTag(otelVariables.IsCallback, command.IsCallback)
            .AddEvent("Integration event ready for dispatcher");

        logger.Information("Integration event ready for dispatcher.");
    }

    public void FailOnRetrieveIntegrationEvent(RouteIntegrationEventRequestCommand command)
    {
        _activity?.SetError("Integration event not founded");
        logger.Error("Integration event not founded.");
    }

    public void IntegrationEventLoaded(IntegrationEvent integrationEvent)
    {
        var squad = _squadsSingleton.GetByEntity(integrationEvent.Entity.Entity);

        _activity?
            .AddTag(otelVariables.Destiny, integrationEvent.Entity.Destiny.Name)
            .AddTag(otelVariables.Origin, integrationEvent.Entity.Origin.Name)
            .AddTag(otelVariables.Entity, integrationEvent.Entity.Entity)
            .AddTag(otelVariables.Squad, squad)
            .AddTag(otelVariables.AggregateKey, integrationEvent.Entity.AggregateKey)
            .AddTag(otelVariables.ControlCode, integrationEvent.ControlCode)
            .AddTag(otelVariables.IntegrationEventId, integrationEvent.Id)
            .AddTag(otelVariables.IsCallback, integrationEvent.Control.IsCallback)
            .AddEvent("Integration event loaded");

        logger.Debug("Integration event loaded.");
    }

    public void ErrorOnDispatcher(IntegrationEvent integrationEvent, DispatcherResult dispatcherResult)
    {
        _activity?
            .AddTag(otelVariables.ErrorStatusCode, dispatcherResult.StatusCode)
            .AddTag(otelVariables.ErrorBody, dispatcherResult.Body)
            .SetError("Error on dispatching integration event");

        var measurement = PlatformMeasurement.Create(
            integrationEvent.Entity.Origin.Name,
            integrationEvent.Entity.Destiny.Name,
            integrationEvent.Entity.Entity,
            _squadsSingleton.GetByEntity(integrationEvent.Entity.Entity),
            integrationEvent.Control.IsCallback);
        platformMetrics.IntegrationFailure(measurement);
        platformMetrics.DestinationFailure(measurement);
    }

    public void DispatcherNeedsRetry(IntegrationEvent integrationEvent, string message)
    {
        _activity?.SetError("Integration event needs retry action");

        var squad = _squadsSingleton.GetByEntity(integrationEvent.Entity.Entity);

        platformMetrics.DestinationFailure(PlatformMeasurement.Create(
            integrationEvent.Entity.Origin.Name, 
            integrationEvent.Entity.Destiny.Name, 
            integrationEvent.Entity.Entity,
            squad,
            integrationEvent.Control.IsCallback));

        logger.Warning("Integration event needs retry action. | Error: {error}", message);
    }

    public void IntegrationEventDispatched(IntegrationEvent integrationEvent)
    {
        _activity?.SetSuccess("Integration event successufly integrated");

        var squad = _squadsSingleton.GetByEntity(integrationEvent.Entity.Entity);

        platformMetrics.SuccessIntegration(PlatformMeasurement.Create(
            integrationEvent.Entity.Origin.Name, 
            integrationEvent.Entity.Destiny.Name, 
            integrationEvent.Entity.Entity,
            squad,
            integrationEvent.Control.IsCallback));
        
        logger.Information("Integration event successufly integrated.");
    }

    public void CriticalFail(Exception exception, RouteIntegrationEventRequestCommand command)
    {
        _activity?.AddExceptionAndEvent(exception.Message, exception);
        _activity?.SetError(exception.Message);

        var squad = _squadsSingleton.GetByEntity(command.Entity);

        logger.Error(exception, "Integration event fail. | Error: {error}.", exception.Message);
        
        platformMetrics.IntegrationFailure(PlatformMeasurement.Create(
            command.Origin, 
            command.Destiny, 
            command.Entity,
            squad,
            command.IsCallback));
    }

    public void FailOnRetrieveRoute(RouteIntegrationEventRequestCommand command)
    {
        _activity?.SetError("Route not found.");
        logger.Error("Route not found.");
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}
