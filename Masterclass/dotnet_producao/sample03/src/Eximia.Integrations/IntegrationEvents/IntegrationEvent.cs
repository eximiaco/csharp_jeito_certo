using Silverback.Domain;
using Eximia.Integrations.Shared;
using Eximia.Integrations.IntegrationEvents.Actions;
using IAggregateRoot = Silverback.Domain.IAggregateRoot;

namespace Eximia.Integrations.IntegrationEvents;

public sealed class IntegrationEvent : DomainEntity, IAggregateRoot
{
    private IntegrationEvent()
    {
    }

    public IntegrationEvent(
        int id,
        EntityInfo entity,
        string controlCode,
        ControlInfo control,
        RetryInfo retry,
        Guid uniqueId,
        bool isPayloadLink,
        string payloadFile,
        Guid fileId,
        string? sapQueryAction,
        int? statusCode)
    {
        Id = id;
        Entity = entity;
        ControlCode = controlCode;
        Control = control;
        Retry = retry;
        UniqueId = uniqueId;
        IsPayloadLink = isPayloadLink;
        PayloadFile = payloadFile;
        PayloadFileId = fileId;
        SapQueryAction = sapQueryAction;
        StatusCode = statusCode;
    }

    public int Id { get; private set; }
    public EntityInfo Entity { get; private set; } = null!;
    public string ControlCode { get; } = string.Empty;
    public ControlInfo Control { get; private set; } = null!;
    public RetryInfo Retry { get; private set; } = null!;
    public Guid UniqueId { get; }
    public bool IsPayloadLink { get; set; }
    public string PayloadFile { get; set; } = string.Empty;
    public Guid PayloadFileId { get; set; }
    public string? SapQueryAction { get; }
    public int? StatusCode { get; }

    public static ResultAggregate<IntegrationEvent> CreateNewIntegrationEvent(
        string entity,
        string origin,
        string destiny,
        string destinyKey,
        string method,
        string controlCode,
        string originKey,
        string destinationQueue,
        string? aggregateKey,
        string? sapQueryAction,
        int? statusCode,
        long version,
        bool isCallback = false,
        bool requireCallback = false,
        EMode mode = EMode.PROD,
        bool isCallbackError = false,
        bool isPayloadLink = false,
        string payloadFile = "",
        Guid fileId = default)
    {
        var result = CanCreateIntegrationEvent(entity, origin, originKey, destiny, isCallback);
        if (result.IsFailure)
            return result.ConvertFailure<IntegrationEvent>();
        var integrationEvent = new IntegrationEvent(
            0,
            new EntityInfo(
                entity,
                new Source(origin, originKey),
                new Source(destiny, destinyKey),
                method,
                destinationQueue,
                aggregateKey ?? string.Empty,
                false,
                version),
            controlCode,
            new ControlInfo(EIntegrationStatus.InOutbox, DateTime.UtcNow, null, null, isCallback, false, mode, isCallbackError),
            new RetryInfo(null, 0, string.Empty),
            uniqueId: Guid.NewGuid(),
            isPayloadLink,
            payloadFile,
            fileId,
            sapQueryAction,
            statusCode);
        if (!requireCallback)
            return integrationEvent;
        if (integrationEvent.ConfigureForRequireCallback() is { IsFailure: true } configureResult)
            return configureResult.ConvertFailure<IntegrationEvent>();
        return integrationEvent;
    }

    private static ResultAggregate CanCreateIntegrationEvent(
        string entity,
        string origin,
        string originKey,
        string destiny,
        bool isCallBack = false)
    {
        return ResultAggregate.Combine(
            ResultAggregate.FailureIf(string.IsNullOrEmpty(destiny), "[Destiny]", "Destiny is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(origin), "[Origin]", "Origin is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(entity), "[Entity]", "Entity is invalid"),
            ResultAggregate.FailureIf(!isCallBack && string.IsNullOrEmpty(originKey), "[OriginKey]", "Origin Key is invalid"));
    }

    public void SetDatabaseId(int id)
    {
        Id = id;
    }

    public ResultAggregate ConfigureForRequireCallback()
    {
        if (Control.IsCallback)
            return ResultAggregate.Failure(
                "[RequireCallback]",
                "Require callback is not allowed for callback integration event");

        Entity.CallbackRequired = true;
        return ResultAggregate.Success();
    }

    public void RegisterFailure(string message)
    {
        Control = Control with { IntegrationStatus = EIntegrationStatus.Error, EndedAt = DateTime.UtcNow };
        Retry = new RetryInfo(DateTime.UtcNow, Retry.Attempts + 1, message);
    }

    public void RegisterRetry(string message)
    {
        Control = Control with { IntegrationStatus = EIntegrationStatus.InOutbox };
        Retry = new RetryInfo(DateTime.UtcNow, Retry.Attempts + 1, message);
    }

    public void StartDispatching()
    {
        Control.IntegrationStatus = EIntegrationStatus.Processing;
        Control.StartedAt = DateTime.UtcNow;
    }

    public void RegisterSuccessOnDispatching()
    {
        if (Control.IsCallback)
            Control.IntegrationStatus = EIntegrationStatus.Processed;
        else if (Entity.CallbackRequired)
            Control.IntegrationStatus = EIntegrationStatus.WaitingCallback;
        else
            Control.IntegrationStatus = EIntegrationStatus.Processed;

        Control.EndedAt = DateTime.UtcNow;
    }

    public PosRegisterIntegrationEventActions GetActionsForPosRegister()
    {
        return Control.IsCallback
            ? PosRegisterIntegrationEventActions.CreateCallbackPolicy()
            : PosRegisterIntegrationEventActions.CreateEmpty();
    }

    public string BuildPayload()
    {
        var payload = new
        {
            metadata = new
            {
                origin = new
                {
                    name = Entity.Origin.Name,
                    key = Entity.Origin.Key
                },
                destiny = new
                {
                    name = Entity.Destiny.Name,
                    key = Entity.Destiny.Key
                },
                entity = new
                {
                    name = Entity.Entity,
                    version = Entity.Version,
                    action = Entity.Method,
                    aggregateKey = Entity.AggregateKey
                },
                correlationKey = ControlCode,
                statusCode = StatusCode,
                isCallback = Control.IsCallback,
                isCallbackError = Control.IsCallbackError,
                sapQueryAction = SapQueryAction,
                destinationQueue = Entity.DestinationQueue
            },
            data = new
            {
                fileName = PayloadFile
            }
        };

        return payload.ToJson();
    }
}

public record RetryInfo(DateTime? LastTime, int Attempts, string LastError);

public record ControlInfo
{
    public ControlInfo(
        EIntegrationStatus integrationStatus,
        DateTime createdAt,
        DateTime? startedAt,
        DateTime? endedAt,
        bool isCallback,
        bool isSync,
        EMode mode,
        bool isCallbackError)
    {
        IntegrationStatus = integrationStatus;
        CreatedAt = createdAt;
        StartedAt = startedAt;
        EndedAt = endedAt;
        IsCallback = isCallback;
        IsSync = isSync;
        Mode = mode;
        IsCallbackError = isCallbackError;
    }

    public EIntegrationStatus IntegrationStatus { get; set; }
    public DateTime CreatedAt { get; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsCallback { get; }
    public bool IsSync { get; }
    public EMode Mode { get; }
    public bool IsCallbackError { get; }

    public bool IsDev() => Mode == EMode.DEV;
}
