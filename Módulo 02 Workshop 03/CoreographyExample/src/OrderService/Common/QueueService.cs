using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace OrderService.Common;
public class QueueService(IConnection connection) : IDisposable
{
    private IChannel? _channel;

    public async Task Subscribe<T>(string queue, Action<T> handler)
    {
        _channel ??= await connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync("order-service-exchange", ExchangeType.Fanout, true);

        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            using MemoryStream ms = new(body);
            var message = await JsonSerializer.DeserializeAsync<T>(ms)
                ?? throw new InvalidOperationException("Failed to deserialize message");

            var delay = new Random().Next(2000, 5000);
            await Task.Delay(delay);

            handler(message);
        };

        await _channel.BasicConsumeAsync(
            queue,
            autoAck: true,
            consumer: consumer);
    }

    public async Task PublishAsync<T>(string queue, T @event)
    {
        await PublishAsync(queue, JsonSerializer.SerializeToUtf8Bytes(@event));
    }

    public async Task PublishAsync(string queue, string payload)
    {
        await PublishAsync(queue, Encoding.UTF8.GetBytes(payload));
    }

    private async Task PublishAsync(string queue, byte[] payload)
    {
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queue, body: payload);
    }

    ~QueueService()
    {
        _channel?.Dispose();
    }

    public void Dispose()
    {
        _channel?.Dispose();
        GC.SuppressFinalize(this);
    }
}
