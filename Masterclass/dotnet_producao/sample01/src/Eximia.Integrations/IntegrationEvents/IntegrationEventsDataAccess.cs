using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using Npgsql;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Extensions;
using Eximia.Integrations.Shared.Postgres;

namespace Eximia.Integrations.IntegrationEvents;

public sealed class IntegrationEventsDataAccess(NpgsqlDataSource dataSource)
{
    public async Task InsertAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO integration_events (
                                entity_type,
                                control_code,
                                origin,
                                origin_key,
                                destiny,
                                destiny_key,
                                method,
                                status,
                                create_time,
                                start_time,
                                end_time,
                                is_callback,
                                retry_last_time,
                                retry_attempts,
                                retry_last_error,
                                destination_queue,
                                aggregate_key,
                                callback_required,
                                is_sync,
                                mode,
                                is_callback_error,
                                is_payload_link,
                                payload_file,
                                file_id,
                                entity_version,
                                sap_query_action,
                                status_code) 
                            VALUES (
                                @EntityType,
                                @ControlCode,
                                @Origin,
                                @OriginKey,
                                @Destiny,
                                @DestinyKey,
                                @Method,
                                @Status,
                                @CreatedAt,
                                @StartedAt,
                                @EndedAt,
                                @IsCallback,
                                @RetryLastTime,
                                @Attempts,
                                @LastError,
                                @DestinationQueue,
                                @AggregateKey,
                                @CallbackRequired,
                                @IsSync,
                                @Mode,
                                @IsCallbackError,
                                @IsPayloadLink,
                                @PayloadFile,
                                @PayloadFileId,
                                @Version,
                                @SapQueryAction,
                                @StatusCode) RETURNING Id";

        var param = new DynamicParameters();
        param.Add("@Id", integrationEvent.Id, DbType.Int32, ParameterDirection.Input);
        param.Add("@EntityType", integrationEvent.Entity.Entity, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@ControlCode", integrationEvent.ControlCode, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@Origin", integrationEvent.Entity.Origin.Name, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@OriginKey", integrationEvent.Entity.Origin.Key, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@Destiny", integrationEvent.Entity.Destiny.Name, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@DestinyKey", integrationEvent.Entity.Destiny.Key, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@Method", integrationEvent.Entity.Method, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@Status", integrationEvent.Control.IntegrationStatus, DbType.Int16, ParameterDirection.Input);
        param.Add("@CreatedAt", integrationEvent.Control.CreatedAt, DbType.DateTime, ParameterDirection.Input);
        param.Add("@StartedAt", integrationEvent.Control.StartedAt, DbType.DateTime, ParameterDirection.Input);
        param.Add("@EndedAt", integrationEvent.Control.EndedAt, DbType.DateTime, ParameterDirection.Input);
        param.Add("@IsCallback", integrationEvent.Control.IsCallback, DbType.Boolean, ParameterDirection.Input);
        param.Add("@RetryLastTime", integrationEvent.Retry.LastTime, DbType.DateTime, ParameterDirection.Input);
        param.Add("@Attempts", integrationEvent.Retry.Attempts, DbType.Int16, ParameterDirection.Input);
        param.Add("@LastError", integrationEvent.Retry.LastError, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@DestinationQueue", integrationEvent.Entity.DestinationQueue, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@AggregateKey", integrationEvent.Entity.AggregateKey, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@CallbackRequired", integrationEvent.Entity.CallbackRequired, DbType.Boolean, ParameterDirection.Input);
        param.Add("@IsSync", integrationEvent.Control.IsSync, DbType.Boolean, ParameterDirection.Input);
        param.Add("@Mode", integrationEvent.Control.Mode, DbType.Int16, ParameterDirection.Input);
        param.Add("@IsCallbackError", integrationEvent.Control.IsCallbackError, DbType.Boolean, ParameterDirection.Input);
        param.Add("@IsPayloadLink", integrationEvent.IsPayloadLink, DbType.Boolean, ParameterDirection.Input);
        param.Add("@PayloadFile", integrationEvent.PayloadFile, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@PayloadFileId", integrationEvent.PayloadFileId, DbType.Guid, ParameterDirection.Input);
        param.Add("@Version", integrationEvent.Entity.Version, DbType.Int64, ParameterDirection.Input);
        param.Add("@SapQueryAction", integrationEvent.SapQueryAction, DbType.AnsiString, ParameterDirection.Input);
        param.Add("@StatusCode", integrationEvent.StatusCode, DbType.Int32, ParameterDirection.Input);

        await using var connection = dataSource.CreateConnection();
        // PROBLEMA: o método recebe o token e não o repassa ao driver. A query não é cancelada e segue no Postgres.
        var result = await connection.ExecuteScalarAsync<int>(sql, param);
        integrationEvent.SetDatabaseId(result);
    }

    private const string _eventColumns = @"id as Id,
                        entity_type as EntityType,
                        control_code as ControlCode,
                        origin as Origin,
                        origin_key as OriginKey,
                        destiny as Destination,
                        destiny_key as DestinationKey,
                        is_callback as IsCallback,
                        status as Status,
                        create_time as CreateTime,
                        start_time as StartTime,
                        end_time as EndTime,
                        retry_last_time as LastRetry,
                        retry_attempts as Retries,
                        retry_last_error as ErrorMessage,
                        method as Method,
                        destination_queue as DestinationQueue,
                        aggregate_key as AggregateKey,
                        callback_required as CallbackRequired,
                        is_sync as IsSync,
                        mode as Mode,
                        is_callback_error as IsCallbackError,
                        unique_id as UniqueId,
                        is_payload_link as IsPayloadLink,
                        payload_file as PayloadFile,
                        file_id as PayloadFileId,
                        entity_version AS Version,
                        sap_query_action AS SapQueryAction,
                        status_code AS StatusCode";

    public async Task<Maybe<IntegrationEvent>> GetAndSetEventToDispatch(Guid uniqueId, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        DynamicParameters param = new();
        param.Add("uniqueId", uniqueId, DbType.Guid, ParameterDirection.Input);
        param.Add("Status", EIntegrationStatus.Processing, DbType.Int16, ParameterDirection.Input);
        param.Add("StartTime", startTime, DbType.DateTime, ParameterDirection.Input);

        // Um único comando: marca o evento como Processing e devolve a linha já atualizada.
        await using var connection = dataSource.CreateConnection();
        var dto = await connection
            // PROBLEMA: o método recebe o token e não o repassa ao driver. A query não é cancelada e segue no Postgres.
            .QueryFirstOrDefaultAsync<EventDto>(
                $@"UPDATE public.integration_events
                      SET
                          status = @Status,
                          start_time = @StartTime
                      WHERE unique_id = @uniqueId
                      RETURNING {_eventColumns};",
                param);

        if (dto == null)
            return Maybe<IntegrationEvent>.None;

        var integrationEvent = ToIntegrationEvent(dto);

        // Mesmo efeito de IntegrationEvent.StartDispatching(), com o mesmo horário gravado no banco.
        integrationEvent.Control.IntegrationStatus = EIntegrationStatus.Processing;
        integrationEvent.Control.StartedAt = startTime;
        return integrationEvent;
    }

    private static IntegrationEvent ToIntegrationEvent(EventDto dto)
        => new IntegrationEvent(
            (int)dto.Id,
            new EntityInfo(
                dto.EntityType,
                new Source(dto.Origin, dto.OriginKey),
                new Source(dto.Destination, dto.DestinationKey),
                dto.Method,
                dto.DestinationQueue,
                dto.AggregateKey,
                dto.CallbackRequired,
                dto.Version),
            dto.ControlCode,
            new ControlInfo(
                (EIntegrationStatus)dto.Status,
                dto.CreateTime,
                dto.StartTime,
                dto.EndTime,
                dto.IsCallback,
                dto.IsSync,
                (EMode)dto.Mode,
                dto.IsCallbackError),
            new RetryInfo(dto.LastRetry, dto.Retries, dto.ErrorMessage ?? string.Empty),
            dto.UniqueId,
            dto.IsPayloadLink,
            dto.PayloadFile,
            dto.PayloadFileId ?? Guid.Empty,
            dto.SapQueryAction,
            dto.StatusCode);

    // PROBLEMA: nem recebe o token. Não há como cancelar, por mais que o chamador tenha um.
    public async Task UpdateIntegrationProcess(IntegrationEvent integrationEvent)
    {
        const string sql = @"UPDATE public.integration_events SET
                                status = @Status,
                                end_time = @EndedAt,
                                retry_last_time = @RetryLastTime,
                                retry_attempts = @Attempts,
                                retry_last_error = @LastError
                            WHERE id = @Id";
        var param = new DynamicParameters();
        param.Add("@Id", integrationEvent.Id, DbType.Int32, ParameterDirection.Input);
        param.Add("@Status", integrationEvent.Control.IntegrationStatus, DbType.Int16, ParameterDirection.Input);
        param.Add("@EndedAt", integrationEvent.Control.EndedAt.SpecifyKindUtc(), DbType.DateTime, ParameterDirection.Input);
        param.Add("@RetryLastTime", integrationEvent.Retry.LastTime.SpecifyKindUtc(), DbType.DateTime, ParameterDirection.Input);
        param.Add("@Attempts", integrationEvent.Retry.Attempts, DbType.Int16, ParameterDirection.Input);
        param.Add("@LastError", integrationEvent.Retry.LastError, DbType.AnsiString, ParameterDirection.Input);

        await using var connection = dataSource.CreateConnection();
        // PROBLEMA: query sem token (o método nem o recebe).
        await connection.ExecuteScalarAsync<int>(sql, param);
    }

    public async Task FinalizeOriginalIntegrationEventsForCallback(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        const string sql = @"UPDATE public.integration_events SET
                                status = 3
                            WHERE control_code = @ControlCode AND status = @Status AND is_callback = false";
        var param = new DynamicParameters();
        param.Add("@Status", EIntegrationStatus.WaitingCallback, DbType.Int16, ParameterDirection.Input);
        param.Add("@ControlCode", integrationEvent.ControlCode, DbType.AnsiString, ParameterDirection.Input);
        await using var connection = dataSource.CreateConnection();
        // PROBLEMA: o método recebe o token e não o repassa ao driver. A query não é cancelada e segue no Postgres.
        await connection.ExecuteAsync(sql, param);
    }

    public async Task<Maybe<IntegrationEvent>> GetAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();
        var dto = await connection
            // PROBLEMA: o método recebe o token e não o repassa ao driver. A query não é cancelada e segue no Postgres.
            .QueryFirstOrDefaultAsync<EventDto>(
                $@"SELECT {_eventColumns}
                    FROM public.integration_events
                    WHERE id = @id;",
                new { id });

        if (dto == null)
            return Maybe<IntegrationEvent>.None;

        return ToIntegrationEvent(dto);
    }

    // PROBLEMA: nem recebe o token. Não há como cancelar, por mais que o chamador tenha um.
    public async Task UpdateKafkaOffsetAsync(Guid uniqueId, long queueOffset, int queuePartition)
    {
        var sql = @"UPDATE integration_events SET queue_offset = @queueOffset, queue_partition = @queuePartition
                    WHERE unique_id = @uniqueId";

        DynamicParameters param = new();
        param.Add("uniqueId", uniqueId, DbType.Guid, ParameterDirection.Input);
        param.Add("queueOffset", queueOffset, DbType.Int64, ParameterDirection.Input);
        param.Add("queuePartition", queuePartition, DbType.Int32, ParameterDirection.Input);

        await using var connection = dataSource.CreateConnection();
        // PROBLEMA: query sem token (o método nem o recebe).
        await connection.ExecuteAsync(sql, param).ConfigureAwait(false);
    }

    public async Task UpdatePayloadAsync(
        int id,
        string payload,
        bool isPayloadLink,
        string payloadFile,
        Guid payloadFileId,
        CancellationToken cancellationToken = default)
    {
        var sql = @"UPDATE integration_events SET payload = @payload, is_payload_link = @isPayloadLink,
                        payload_file = @payloadFile, file_id = @payloadFileId
                    WHERE id = @id";

        DynamicParameters param = new();
        param.Add("id", id, DbType.Int32);
        param.Add("payload", new JsonParameter(payload), DbType.String);
        param.Add("isPayloadLink", isPayloadLink, DbType.Boolean);
        param.Add("payloadFile", payloadFile, DbType.AnsiString);
        param.Add("payloadFileId", payloadFileId, DbType.Guid);

        await using var connection = dataSource.CreateConnection();
        // PROBLEMA: o método recebe o token e não o repassa ao driver. A query não é cancelada e segue no Postgres.
        await connection.ExecuteAsync(sql, param);
    }

    public async Task<Maybe<string>> GetDestinyAsync(int id, CancellationToken cancellationToken)
    {
        var sql = @"SELECT destiny
                    FROM integration_events
                    WHERE id = @id";
        
        DynamicParameters param = new();
        param.Add("id", id, DbType.Int32);
        
        await using var connection = dataSource.CreateConnection();
        // PROBLEMA: o método recebe o token e não o repassa ao driver. A query não é cancelada e segue no Postgres.
        var destiny = await connection.QueryFirstOrDefaultAsync<string>(sql, param);
        
        return string.IsNullOrEmpty(destiny) ? Maybe.None : destiny;
    }

    public record EventDto(
        long Id,
        string EntityType,
        string ControlCode,
        string? Origin,
        string? OriginKey,
        string? Destination,
        string? DestinationKey,
        bool IsCallback,
        int Status,
        DateTime CreateTime,
        DateTime? StartTime,
        DateTime? EndTime,
        DateTime? LastRetry,
        int Retries,
        string? ErrorMessage,
        string Method,
        string DestinationQueue,
        string AggregateKey,
        bool CallbackRequired,
        bool IsSync,
        int Mode,
        bool IsCallbackError,
        Guid UniqueId,
        bool IsPayloadLink,
        string PayloadFile,
        Guid? PayloadFileId,
        long? Version,
        string? SapQueryAction,
        int? StatusCode);
}
