using GymErp.Common;
using GymErp.Common.Infrastructure;
using GymErp.Common.RabbitMq;
using GymErp.Domain.Financial.Features.ProcessCharging.Consumers;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;
using Silverback.Messaging.Configuration;

namespace GymErp.Bootstrap;

internal static class SilverbackServiceExtensions
{
    static SilverbackServiceExtensions()
    {
        MessageBrokerRegistry.Register("Silverback", (s, c) => s.AddSilverbackBroker(c));
    }

    public static IServiceCollection AddSilverbackBroker(this IServiceCollection services, IConfiguration configuration)
    {
        AddSilverbackRabbitMq(services, configuration);
        services.AddScoped<IServiceBus, SilverbackServiceBus>();
        return services;
    }

    public static IServiceCollection AddSilverbackRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqConfig>(configuration.GetSection("RabbitMq"));

        services
            .AddSilverback()
            .WithConnectionToMessageBroker(config => config.AddRabbit())
            .AddEndpointsConfigurator<RabbitMqEndpointsConfigurator>()
            .AddScopedSubscriber<EnrollmentCreatedEventConsumer>()
            .AddScopedSubscriber<CancelEnrollmentCommandSilverbackConsumer>();

        return services;
    }
}
