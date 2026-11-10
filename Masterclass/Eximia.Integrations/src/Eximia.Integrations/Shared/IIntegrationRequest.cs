namespace Eximia.Integrations.Shared;

public interface IIntegrationRequest
{
    public string Origin { get; }
    public string OriginKey { get; }
    public string DestinyKey { get; }
    public string CorrelationId { get; }
    public int SchemaVersion { get; }
    public bool IsCallBack { get; }
}