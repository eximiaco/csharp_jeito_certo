using Confluent.Kafka;
using System.Text.Json;

namespace FinanceManager.Infra
{
    public interface IMessageBroker
    {
        Task ProduceMessageAsync(object message);
    }

    public class MessageBrokerService(IProducer<Null, string> producer) : IMessageBroker
    {
        public async Task ProduceMessageAsync(object message)
        {
            await producer.ProduceAsync("finance.control.events", new Message<Null, string> { Value = JsonSerializer.Serialize(message) });
        }
    }
}
