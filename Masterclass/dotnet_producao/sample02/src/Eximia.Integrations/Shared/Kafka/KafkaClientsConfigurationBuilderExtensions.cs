using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Eximia.Integrations.Shared.Kafka;

public static class KafkaClientsConfigurationBuilderExtensions
{
    /// <summary>
    ///     Aplica a conexão (bootstrap servers + segurança) definida em <see cref="KafkaConfig" /> a todos os clientes Kafka.
    /// </summary>
    public static KafkaClientsConfigurationBuilder WithEximiaConnection(
        this KafkaClientsConfigurationBuilder builder,
        KafkaConfig config)
    {
        switch (config.Connection.SecurityProtocol)
        {
            case SecurityProtocol.Plaintext:
                return builder
                    .WithBootstrapServers(config.Connection.BootstrapServers!)
                    .WithSecurityProtocol(SecurityProtocol.Plaintext);
            case SecurityProtocol.Ssl:
                throw new Exception("Invalid security protocol Ssl");
            case SecurityProtocol.SaslPlaintext:
                throw new Exception("Invalid security protocol Sasl Plain Text");
            case SecurityProtocol.SaslSsl:
                return builder
                    .WithBootstrapServers(config.Connection.BootstrapServers!)
                    .WithSaslUsername(config.Connection.Username!)
                    .WithSaslPassword(config.Connection.Password!)
                    .WithSaslMechanism(SaslMechanism.Plain)
                    .WithSecurityProtocol(SecurityProtocol.SaslSsl);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
