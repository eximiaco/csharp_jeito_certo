using GymErp.Common;
using GymErp.Common.Infrastructure;
using GymErp.Common.RabbitMq;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Financial.Features.ProcessCharging.Consumers;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;
using MassTransit;

namespace GymErp.Bootstrap;

internal static class MassTransitServiceExtensions
{
    static MassTransitServiceExtensions()
    {
        MessageBrokerRegistry.Register("MassTransit", (s, c) => s.AddMassTransitBroker(c));
    }

    public static IServiceCollection AddMassTransitBroker(this IServiceCollection services, IConfiguration configuration)
    {
        AddMassTransitRabbitMq(services, configuration);
        services.AddScoped<IServiceBus, MassTransitServiceBus>();
        return services;
    }

    public static IServiceCollection AddMassTransitRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqSection = configuration.GetSection("RabbitMq");
        var rabbitMqConfig = new RabbitMqConfig();
        rabbitMqConfig.Connection = rabbitMqSection.GetSection("Connection").Get<RabbitMqConnectionConfig>() ?? new RabbitMqConnectionConfig();
        services.AddSingleton(rabbitMqConfig);

        var conn = rabbitMqConfig.Connection;

        services.AddMassTransit(x =>
        {
            x.AddConsumer<EnrollmentCreatedEventMassTransitConsumer>();
            x.AddConsumer<CancelEnrollmentCommandMassTransitConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(conn.HostName, conn.VirtualHost ?? "/", h =>
                {
                    h.Username(conn.UserName);
                    h.Password(conn.Password);
                });

                cfg.Message<EnrollmentCreatedEvent>(m => m.SetEntityName("enrollment-events"));
                cfg.Message<ChargingProcessedEvent>(m => m.SetEntityName("charging-processed-events"));
                cfg.Message<CancelEnrollmentCommand>(m => m.SetEntityName("cancel-enrollment-commands"));

                cfg.ReceiveEndpoint("enrollment-events-financial-module", e =>
                {
                    e.ConfigureConsumer<EnrollmentCreatedEventMassTransitConsumer>(context);
                });
                cfg.ReceiveEndpoint("cancel-enrollment-commands-subscriptions-module", e =>
                {
                    e.ConfigureConsumer<CancelEnrollmentCommandMassTransitConsumer>(context);
                });
            });
        });

        return services;
    }
}
