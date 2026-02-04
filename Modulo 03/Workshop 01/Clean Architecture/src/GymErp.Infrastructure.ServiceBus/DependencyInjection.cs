using GymErp.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Infrastructure.ServiceBus;

public static class DependencyInjection
{
    public static IServiceCollection AddEventPublisher(this IServiceCollection services)
    {
        services.AddScoped<IEventPublisher, NoOpEventPublisher>();
        return services;
    }
}
