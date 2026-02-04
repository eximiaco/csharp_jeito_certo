using System.Text.Json;
using Confluent.Kafka;
using GymErp.Common.Infrastructure;
using GymErp.Common.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CancelEnrollmentHandler = GymErp.Domain.Subscriptions.Features.CancelEnrollment.Handler;

namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;

public sealed class CancelEnrollmentCommandConsumerHostedService(
    IOptions<KafkaConfig> kafkaOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<CancelEnrollmentCommandConsumerHostedService> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private IConsumer<Ignore, byte[]>? _consumer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = kafkaOptions.Value;
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config.BootstrapServers,
            GroupId = KafkaTopics.SubscriptionsModuleGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<Ignore, byte[]>(consumerConfig).Build();
        _consumer.Subscribe(KafkaTopics.CancelEnrollmentCommands);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result?.Message?.Value == null)
                        continue;

                    var message = JsonSerializer.Deserialize<CancelEnrollmentCommand>(result.Message.Value, JsonOptions);
                    if (message == null)
                    {
                        logger.LogWarning("Failed to deserialize CancelEnrollmentCommand");
                        continue;
                    }

                    await using (var scope = scopeFactory.CreateAsyncScope())
                    {
                        var handler = scope.ServiceProvider.GetRequiredService<CancelEnrollmentHandler>();
                        await handler.HandleAsync(message, stoppingToken).ConfigureAwait(false);
                    }

                    _consumer.StoreOffset(result);
                    _consumer.Commit();
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming CancelEnrollmentCommand: {Reason}", ex.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            _consumer.Close();
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
