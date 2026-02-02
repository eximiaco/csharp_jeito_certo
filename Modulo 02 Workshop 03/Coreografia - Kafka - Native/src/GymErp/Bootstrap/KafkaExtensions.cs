using Confluent.Kafka;
using GymErp.Common;
using GymErp.Common.Infrastructure;
using GymErp.Common.Kafka;
using GymErp.Domain.Financial.Features.ProcessCharging.Consumers;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GymErp.Bootstrap;

internal static class KafkaExtensions
{
    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaConfig>(configuration.GetSection("Kafka"));

        services.AddSingleton<IProducer<Null, byte[]>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaConfig>>().Value;
            var config = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers
            };
            return new ProducerBuilder<Null, byte[]>(config).Build();
        });

        services.AddScoped<IServiceBus, KafkaServiceBus>();

        services.AddHostedService<EnrollmentCreatedEventConsumerHostedService>();
        services.AddHostedService<CancelEnrollmentCommandConsumerHostedService>();

        return services;
    }
}
