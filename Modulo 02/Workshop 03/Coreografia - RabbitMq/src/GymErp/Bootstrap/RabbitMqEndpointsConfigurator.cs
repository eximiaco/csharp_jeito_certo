using GymErp.Common.RabbitMq;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Rabbit;

namespace GymErp.Bootstrap;

public class RabbitMqEndpointsConfigurator(IOptions<RabbitMqConfig> options) : IEndpointsConfigurator
{
    private readonly RabbitMqConnectionConfig _connection = options.Value.Connection;

    private static RabbitConnectionConfig ToSilverbackConnection(RabbitMqConnectionConfig c)
    {
        return new RabbitConnectionConfig
        {
            HostName = c.HostName,
            Port = c.Port,
            UserName = c.UserName,
            Password = c.Password,
            VirtualHost = c.VirtualHost
        };
    }

    private static RabbitExchangeConfig FanoutExchange() => new()
    {
        IsDurable = true,
        IsAutoDeleteEnabled = false,
        ExchangeType = global::RabbitMQ.Client.ExchangeType.Fanout
    };

    private static RabbitQueueConfig DurableQueue() => new()
    {
        IsDurable = true,
        IsExclusive = false,
        IsAutoDeleteEnabled = false
    };

    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        var conn = ToSilverbackConnection(_connection);

        builder
            .AddOutbound<EnrollmentCreatedEvent>(new RabbitExchangeProducerEndpoint("enrollment-events")
            {
                Connection = conn,
                Exchange = FanoutExchange()
            })
            .AddOutbound<ChargingProcessedEvent>(new RabbitExchangeProducerEndpoint("charging-processed-events")
            {
                Connection = conn,
                Exchange = FanoutExchange()
            })
            .AddOutbound<CancelEnrollmentCommand>(new RabbitExchangeProducerEndpoint("cancel-enrollment-commands")
            {
                Connection = conn,
                Exchange = FanoutExchange()
            })
            .AddInbound(new RabbitExchangeConsumerEndpoint("enrollment-events")
            {
                Connection = conn,
                Exchange = FanoutExchange(),
                QueueName = "enrollment-events-financial-module",
                Queue = DurableQueue()
            })
            .AddInbound(new RabbitExchangeConsumerEndpoint("cancel-enrollment-commands")
            {
                Connection = conn,
                Exchange = FanoutExchange(),
                QueueName = "cancel-enrollment-commands-subscriptions-module",
                Queue = DurableQueue()
            });
    }
}
