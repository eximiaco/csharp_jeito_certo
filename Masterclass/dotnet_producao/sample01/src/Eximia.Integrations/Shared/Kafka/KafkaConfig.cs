using Confluent.Kafka;

namespace Eximia.Integrations.Shared.Kafka;

public class KafkaConfig
{
    public KafkaConfig()
    {
        Connection = new KafkaConnectionConfig(string.Empty, Guid.NewGuid().ToString(), 
            SecurityProtocol.Plaintext, string.Empty, string.Empty);
        Consumer = new KafkaConsumerConfig(Array.Empty<KafkaInboundConfig>().ToList());
        Callback = new KafkaCallbackConfig(string.Empty, null);
        Events = new KafkaEventConfig(string.Empty, null);
    }

    public KafkaConnectionConfig Connection { get; set; }
    public KafkaConsumerConfig Consumer { get; set; }
    public KafkaCallbackConfig Callback { get; set; }
    public KafkaEventConfig Events { get; set; }
}

public record KafkaConnectionConfig(string? BootstrapServers, string? GroupId, SecurityProtocol SecurityProtocol, string? Username, string? Password);
public record KafkaConsumerConfig(List<KafkaInboundConfig> Inbounds);
public record KafkaInboundConfig(
    string? Topics,
    int? AutoOffsetReset,
    int? BackpressureLimit = null,
    int? MaxDegreeOfParallelism = null);
public record KafkaCallbackConfig(string Topic, int? AutoOffsetReset);
public record KafkaEventConfig(string Topic, int? AutoOffsetReset);
