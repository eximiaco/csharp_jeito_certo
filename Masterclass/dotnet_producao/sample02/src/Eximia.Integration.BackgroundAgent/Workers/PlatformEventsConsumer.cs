using System.Text.Json;
using Confluent.Kafka;
using Serilog.Events;
using Eximia.Integration.BackgroundAgent.PlatformEvents;
using Eximia.Integrations.PlatformEvents;
using Eximia.Integrations.Shared.Kafka;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>
///     Consome os eventos da plataforma (o tópico que a API e o dispatcher já publicam) e alimenta a linha do tempo
///     das integrações. Usa o client puro da Confluent, seguindo as práticas que ela recomenda:
///
///     <list type="bullet">
///         <item>
///             <b>Uma thread própria.</b> <c>Consume</c> é bloqueante. Rodá-lo no pool seria prender uma thread do
///             ThreadPool para sempre (e, antes do .NET 10, travaria a subida do host, porque o código síncrono do
///             <c>ExecuteAsync</c> rodava no <c>StartAsync</c>). Uma thread <c>LongRunning</c> é nossa: bloquear nela
///             não afeta o pool.
///         </item>
///         <item>
///             <b>At-least-once.</b> <c>EnableAutoCommit</c> fica ligado (o commit roda em segundo plano), mas
///             <c>EnableAutoOffsetStore</c> fica desligado: o offset só é marcado com <c>StoreOffset</c> DEPOIS de
///             processar. Uma mensagem nunca é confirmada antes de tratada.
///         </item>
///         <item><b>Idempotência.</b> A reentrega é normal (rebalance, restart): os handlers precisam tolerar repetição.</item>
///         <item><b>Encerramento limpo.</b> <c>Close()</c> confirma os offsets guardados e sai do grupo na hora, sem esperar o
///         <c>session.timeout</c> para o rebalance.</item>
///         <item><b>Rebalance cooperativo</b> (<c>CooperativeSticky</c>): só as partições que mudam de dono são pausadas.</item>
///         <item><b>Erros.</b> Erro do client é logado e o librdkafka se recupera sozinho; erro fatal derruba o serviço; mensagem
///         que não desserializa é logada e pulada (não pode travar a partição); falha do banco é retentada e, persistindo,
///         derruba o serviço sem guardar o offset.</item>
///     </list>
/// </summary>
public sealed class PlatformEventsConsumer(
    KafkaConfig config,
    IServiceScopeFactory scopes,
    ILogger logger) : BackgroundService
{
    private const int MaxAttempts = 4;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        // Thread dedicada: Consume() bloqueia. O Task devolvido representa a vida do loop; se ele falhar, o host cai.
        => Task.Factory.StartNew(
            () => ConsumeLoop(stoppingToken),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        // Nome do tópico FIXO, vindo do arquivo de configuração (Kafka:Events:Topic).
        var topic = config.Events.Topic;

        // O consumer nasce, é usado e morre nesta thread: o IConsumer não deve ser compartilhado entre threads.
        using var consumer = new ConsumerBuilder<string, string>(BuildConfig())
            .SetErrorHandler((_, error) => logger.Write(
                error.IsFatal ? LogEventLevel.Fatal : LogEventLevel.Warning,
                "Kafka client error: {Reason} (code {Code}, fatal: {IsFatal})", error.Reason, error.Code, error.IsFatal))
            .SetLogHandler((_, message) => logger.Write(
                ToLevel(message.Level), "librdkafka {Facility}: {Message}", message.Facility, message.Message))
            .SetPartitionsAssignedHandler((_, partitions) => logger.Information(
                "Partitions assigned: {Partitions}", string.Join(", ", partitions)))
            .SetPartitionsRevokedHandler((_, partitions) => logger.Information(
                "Partitions revoked: {Partitions}", string.Join(", ", partitions.Select(p => p.TopicPartition))))
            .SetPartitionsLostHandler((_, partitions) => logger.Warning(
                "Partitions lost: {Partitions}", string.Join(", ", partitions.Select(p => p.TopicPartition))))
            .Build();

        try
        {
            consumer.Subscribe(topic);
            logger.Information("Consuming platform events from {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex) when (!ex.Error.IsFatal)
                {
                    // Erro transitório (broker fora, timeout): o client se recupera. Só registramos.
                    logger.Warning(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    continue;
                }

                if (result?.Message is null)
                    continue;

                Process(result, topic, stoppingToken);

                // Só agora a mensagem conta como processada: o auto-commit em segundo plano confirma este offset.
                consumer.StoreOffset(result);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown pedido: saída normal.
        }
        finally
        {
            // Confirma os offsets guardados e sai do grupo, para o rebalance não esperar o session.timeout.
            consumer.Close();
            logger.Information("Platform events consumer stopped");
        }
    }

    private void Process(ConsumeResult<string, string> result, string topic, CancellationToken cancellationToken)
    {
        PlatformEventContext context;
        try
        {
            var envelope = PlatformEventEnvelope.Parse(result.Message.Value);
            context = new PlatformEventContext(envelope, topic, result.Partition.Value, result.Offset.Value);
        }
        catch (JsonException ex)
        {
            // Mensagem venenosa: repetir não adianta. Registra e segue, para não travar a partição inteira.
            logger.Error(ex, "Skipping malformed platform event at {TopicPartitionOffset}", result.TopicPartitionOffset);
            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = scopes.CreateScope();
                // I/O síncrono, de propósito: esta thread é NOSSA (LongRunning), não do ThreadPool, então bloquear
                // aqui não causa starvation. Não há .Result nem GetResult(): nada de async embrulhado em síncrono.
                foreach (var handler in scope.ServiceProvider.GetServices<IPlatformEventHandler>())
                    handler.Handle(context);

                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < MaxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                logger.Warning(ex, "Platform event {TopicPartitionOffset} failed (attempt {Attempt}/{Max}); retrying in {Delay}",
                    result.TopicPartitionOffset, attempt, MaxAttempts, delay);

                // Espera cancelável: o shutdown interrompe o backoff.
                if (cancellationToken.WaitHandle.WaitOne(delay))
                    cancellationToken.ThrowIfCancellationRequested();
            }
            // Na última tentativa a exceção sobe: o serviço cai SEM guardar o offset, e a mensagem é reentregue após
            // o restart. Pular uma falha persistente do banco em silêncio perderia dados.
        }
    }

    private ConsumerConfig BuildConfig()
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config.Connection.BootstrapServers,
            SecurityProtocol = config.Connection.SecurityProtocol,

            // Grupo próprio: não divide partições com os consumers do Silverback.
            GroupId = $"{config.Connection.GroupId}-platform-events-timeline",
            AutoOffsetReset = (AutoOffsetReset)(config.Events.AutoOffsetReset ?? (int)AutoOffsetReset.Earliest),

            // At-least-once: commit automático em segundo plano, mas só do que foi guardado com StoreOffset.
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false,

            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
            EnablePartitionEof = false,
            AllowAutoCreateTopics = false
        };

        if (config.Connection.SecurityProtocol == SecurityProtocol.SaslSsl)
        {
            consumerConfig.SaslMechanism = SaslMechanism.Plain;
            consumerConfig.SaslUsername = config.Connection.Username;
            consumerConfig.SaslPassword = config.Connection.Password;
        }

        return consumerConfig;
    }

    private static LogEventLevel ToLevel(SyslogLevel level) => level switch
    {
        SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogEventLevel.Fatal,
        SyslogLevel.Error => LogEventLevel.Error,
        SyslogLevel.Warning => LogEventLevel.Warning,
        SyslogLevel.Notice or SyslogLevel.Info => LogEventLevel.Information,
        _ => LogEventLevel.Debug
    };
}
