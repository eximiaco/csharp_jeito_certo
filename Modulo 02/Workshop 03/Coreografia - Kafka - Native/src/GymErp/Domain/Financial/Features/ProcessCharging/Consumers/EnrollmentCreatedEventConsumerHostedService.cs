using System.Text.Json;
using Confluent.Kafka;
using GymErp.Common.Infrastructure;
using GymErp.Common.Kafka;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessChargingHandler = GymErp.Domain.Financial.Features.ProcessCharging.Handler;

namespace GymErp.Domain.Financial.Features.ProcessCharging.Consumers;

public sealed class EnrollmentCreatedEventConsumerHostedService(
    IOptions<KafkaConfig> kafkaOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<EnrollmentCreatedEventConsumerHostedService> logger)
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
            GroupId = KafkaTopics.FinancialModuleGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<Ignore, byte[]>(consumerConfig).Build();
        _consumer.Subscribe(KafkaTopics.EnrollmentEvents);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result?.Message?.Value == null)
                        continue;

                    var message = JsonSerializer.Deserialize<EnrollmentCreatedEvent>(result.Message.Value, JsonOptions);
                    if (message == null)
                    {
                        logger.LogWarning("Failed to deserialize EnrollmentCreatedEvent");
                        continue;
                    }

                    await using (var scope = scopeFactory.CreateAsyncScope())
                    {
                        var handler = scope.ServiceProvider.GetRequiredService<ProcessChargingHandler>();
                        await handler.HandleAsync(message, stoppingToken).ConfigureAwait(false);
                    }

                    _consumer.StoreOffset(result);
                    _consumer.Commit();
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming EnrollmentCreatedEvent: {Reason}", ex.Error.Reason);
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
