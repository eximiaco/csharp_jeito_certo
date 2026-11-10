namespace Eximia.Integration.BackgroundAgent;

public static class Ambient
{
    public static string DatabaseConnectionName => "integration_event";
}


public static class LogsProperty
{
    public static string Origin => "IntegrationOrigin";
    public static string OriginKey => "IntegrationOriginKey";
    public static string Destiny => "IntegrationDestiny";
    public static string DestinyKey => "IntegrationDestinyKey";
    public static string Entity => "IntegrationEntity";
    public static string DestinationQueue => "DestinationQueue";
    public static string AggregateKey => "AggregateKey";
    public static string CorrelationId => "IntegrationCorrelationId";
    public static string IntegrationType => "IntegrationCallback";
}
