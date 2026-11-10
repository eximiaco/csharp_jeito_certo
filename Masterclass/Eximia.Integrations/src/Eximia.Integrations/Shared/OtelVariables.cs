namespace Eximia.Integrations.Shared;

public class OtelVariables
{
    public string IntegrationId => "integration-platform.event.id";
    public string IntegrationUniqueId => "integration-platform.event.unique_id";
    public string Destiny => "integration-platform.metadata.destiny";
    public string DestinyKey => "integration-platform.metadata.destiny.key";
    public string Origin => "integration-platform.metadata.origin";
    public string OriginKey => "integration-platform.metadata.origin.key";
    public string Entity => "integration-platform.metadata.entity";
    public string EntityVersion => "integration-platform.metadata.entity.version";
    public string AggregateKey => "integration-platform.metadata.aggregate_key";
    public string Squad => "integration-platform.metadata.squad";

    public string RouteId => "integration-platform.event.route_id";
    public string SchemaId => "integration-platform.event.schema_id";
    public string IntegrationEventId => "integration-platform.event.id";
    public string ControlCode => "integration-platform.event.control_code";
    public string IsCallback => "integration-platform.event.is_callback";
    public string IsCallbackError => "integration-platform.event.is_callback_error";
    public string CallbackRequired => "integration-platform.event.callback_required";
    public string Status => "integration-platform.event.status";
    public string ErrorStatusCode => "integration-platform.event.error_status_code";
    public string ErrorBody => "integration-platform.event.error_body";
    public string ErrorUri => "integration-platform.event.error_uri";
    public string Error => "integration-platform.event.error";

    public string KafkaTopicName => "integration-platform.kafka.topic_name";
    public string KafkaTopicOffset => "integration-platform.kafka.topic_offset";
    public string KafkaMessageKey => "integration-platform.kafka.message_key";
    public string KafkaMessageId => "integration-platform.kafka.Message_id";

    public string RouteAuthUri => "integration-platform.route.auth_uri";
    public string RouteAuthTokenExpires => "integration-platform.route.auth_expires_in";
    public string RouteAuthToken => "integration-platform.route.auth_token";
    public string RouteNewAuthTokenExpires => "integration-platform.route.new_auth_expires_in";
    public string RouteNewAuthToken => "integration-platform.route.new_auth_token";

    public string CallbackErrorStatusCode => "integration-platform.callback_error.status_code";
    public string CallbackErrorMessage => "integration-platform.callback_error.message";
    public string CallbackErrorResponseBody => "integration-platform.callback_error.response_body";
    public string CallbackAuthErrorResponseBody => "integration-platform.callback_error.auth.response_body";

    public string FileName => "integration-platform.file_upload.name";
    public string FileContentType => "integration-platform.file_upload.contenty_type";
    public string FileUri => "integration-platform.file_upload.uri";
    public string FileIntegrationErrorMessage => "integration-platform.file_upload.error_message";
    public string FileIntegrationErrorBody => "integration-platform.file_upload.error_body";
    public string FileIntegrationStatusCode => "integration-platform.file_upload.status_code";
}