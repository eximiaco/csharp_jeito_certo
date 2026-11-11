using System.Collections.Concurrent;
using Confluent.Kafka;
using Eximia.Integrations.PlatformEvents;
using Eximia.Integrations.Shared.Kafka;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>
///     Consome os eventos da plataforma e entrega a quem se inscreveu. É compartilhado por mais de um
///     BackgroundService: cada um chama <see cref="Subscribe" /> e <see cref="StartReceive" />.
/// </summary>
public sealed class PlatformEventsBus(KafkaConfig config, IServiceScopeFactory scopes)
{
    private readonly ConcurrentQueue<Func<PlatformEventContext, Task>> _handlers = new();
    private IConsumer<string, string>? _consumer;
    private bool _started;

    public void Subscribe(Func<PlatformEventContext, Task> handler) => _handlers.Enqueue(handler);

    public void StartReceive()
    {
        // Quem chega depois só pega carona no consumer que já está de pé.
        if (_started)
            return;

        if (_consumer is not null)
            throw new InvalidOperationException(
                "The platform events consumer is already running and needs to be stopped in order to perform this operation.");

        string topic;
        using (var scope = scopes.CreateScope())
            topic = scope.ServiceProvider.GetRequiredService<TopicNameFactoryPeerEnviroment>().Create(config.Events.Topic);

        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = config.Connection.BootstrapServers,
            SecurityProtocol = config.Connection.SecurityProtocol,
            GroupId = $"{config.Connection.GroupId}-platform-events-timeline",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();
        _consumer.Subscribe(topic);

        _started = true;
        Task.Run(ConsumeLoop);
    }

    private void ConsumeLoop()
    {
        while (true)
        {
            var result = _consumer!.Consume();

            // Para "não segurar o consumer", cada mensagem vira uma tarefa solta.
            _ = Task.Run(() => Process(result));
        }
    }

    private void Process(ConsumeResult<string, string> result)
    {
        var context = new PlatformEventContext(
            PlatformEventEnvelope.Parse(result.Message.Value),
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);

        foreach (var handler in _handlers)
            handler(context).GetAwaiter().GetResult();
    }
}
