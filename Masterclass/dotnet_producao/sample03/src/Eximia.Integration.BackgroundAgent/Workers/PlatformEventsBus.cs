using System.Collections.Concurrent;
using Confluent.Kafka;
using Eximia.Integrations.PlatformEvents;
using Eximia.Integrations.Shared.Kafka;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>
///     Consome os eventos da plataforma e entrega a quem se inscreveu. É compartilhado por mais de um
///     BackgroundService: cada um chama <see cref="Subscribe" /> e <see cref="StartReceive" />.
/// </summary>
public sealed class PlatformEventsBus(KafkaConfig config)
{
    private readonly ConcurrentQueue<Func<PlatformEventContext, Task>> _handlers = new();
    private IConsumer<string, string>? _consumer;
    private bool _started;

    public void Subscribe(Func<PlatformEventContext, Task> handler) => _handlers.Enqueue(handler);

    public void StartReceive()
    {
        // Quem chega depois só pega carona no consumer que já está de pé.
        // PROBLEMA: guarda não atômica (check-then-act). Os dois BackgroundService
        // chamam StartReceive no ExecuteAsync.
        // No .NET 8 rodavam em sequência e funcionava; no .NET 10 rodam em paralelo e passam juntos por aqui.
        if (_started)
            return;

        if (_consumer is not null)
            throw new InvalidOperationException("The platform events consumer is already running and needs to be stopped in order to perform this operation.");

        var topic = config.Events.Topic;
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = config.Connection.BootstrapServers,
            SecurityProtocol = config.Connection.SecurityProtocol,
            GroupId = $"{config.Connection.GroupId}-platform-events-timeline",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();
        _consumer.Subscribe(topic);

        _started = true;
        // PROBLEMA: o loop bloqueante roda numa thread do pool.
        // Prende uma thread do ThreadPool para sempre; precisaria de uma thread dedicada.
        Task.Run(ConsumeLoop);
    }

    private void ConsumeLoop()
    {
        while (true)
        {
            // PROBLEMA: Consume() bloqueante, sem token: nunca encerra.
            var result = _consumer!.Consume();

            // Para "não segurar o consumer", cada mensagem vira uma tarefa solta.
            // PROBLEMA: uma task solta por mensagem. Sem limite (a rajada enche a fila do pool),
            // a exceção some e a ordem dentro da partição se perde.
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
            // PROBLEMA: bloqueia a thread do pool esperando o handler.
            handler(context).GetAwaiter().GetResult();
    }
}
