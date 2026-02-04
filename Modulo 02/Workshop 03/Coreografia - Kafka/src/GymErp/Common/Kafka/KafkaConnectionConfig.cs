namespace GymErp.Common.Kafka;

public class KafkaConnectionConfig
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string SecurityProtocol { get; set; } = string.Empty;
    public string SaslMechanism { get; set; } = string.Empty;
    public string SaslUsername { get; set; } = string.Empty;
    public string SaslPassword { get; set; } = string.Empty;
    public bool EnableSslCertificateVerification { get; set; }
    public string ClientId { get; set; } = string.Empty;
}