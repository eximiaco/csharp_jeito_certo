using System.Text.Json;

namespace OrderService.Data;

public class OutboxMessage
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private OutboxMessage() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public OutboxMessage(string queueName, object payload)
    {
        Payload = JsonSerializer.Serialize(payload);
        QueueName = queueName;
    }

    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Payload { get; init; } 
    public string QueueName { get; init; } 
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
