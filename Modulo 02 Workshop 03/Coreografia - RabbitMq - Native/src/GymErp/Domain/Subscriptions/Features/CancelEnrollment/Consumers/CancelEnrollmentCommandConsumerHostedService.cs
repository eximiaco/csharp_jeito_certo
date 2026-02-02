using System.Text.Json;
using GymErp.Common.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CancelEnrollmentHandler = GymErp.Domain.Subscriptions.Features.CancelEnrollment.Handler;

namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;

public sealed class CancelEnrollmentCommandConsumerHostedService(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<CancelEnrollmentCommandConsumerHostedService> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(null, stoppingToken).ConfigureAwait(false);
        await channel.BasicQosAsync(0, 1, false, stoppingToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<CancelEnrollmentCommand>(body, JsonOptions);
                if (message == null)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken).ConfigureAwait(false);
                    return;
                }

                await using (var scope = scopeFactory.CreateAsyncScope())
                {
                    var handler = scope.ServiceProvider.GetRequiredService<CancelEnrollmentHandler>();
                    await handler.HandleAsync(message, stoppingToken).ConfigureAwait(false);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing CancelEnrollmentCommand");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken).ConfigureAwait(false);
            }
        };

        await channel.BasicConsumeAsync(RabbitMqTopology.QueueCancelEnrollmentCommandsSubscriptions, autoAck: false, consumer, stoppingToken).ConfigureAwait(false);

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }
}
