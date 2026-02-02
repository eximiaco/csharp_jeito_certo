using GymErp.Common;
using GymErp.Domain.Financial.Features.ProcessCharging.Consumers;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;
using GymErp.Common.Infrastructure;
using GymErp.Common.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GymErp.Bootstrap;

internal static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqConfig>(configuration.GetSection("RabbitMq"));

        services.AddSingleton<IConnection>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqConfig>>().Value;
            var c = options.Connection;
            var factory = new ConnectionFactory
            {
                HostName = c.HostName,
                Port = c.Port,
                UserName = c.UserName,
                Password = c.Password,
                VirtualHost = c.VirtualHost ?? "/",
                ClientProvidedName = c.ClientProvidedName ?? "GymErp"
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<RabbitMqTopologyStartup>();
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqTopologyStartup>());

        services.AddScoped<IServiceBus, RabbitMqServiceBus>();

        services.AddHostedService<EnrollmentCreatedEventConsumerHostedService>();
        services.AddHostedService<CancelEnrollmentCommandConsumerHostedService>();

        return services;
    }
}

file sealed class RabbitMqTopologyStartup(IConnection connection) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(null, cancellationToken).ConfigureAwait(false);
        await RabbitMqTopology.DeclareExchangesAndQueuesAsync(channel, cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
