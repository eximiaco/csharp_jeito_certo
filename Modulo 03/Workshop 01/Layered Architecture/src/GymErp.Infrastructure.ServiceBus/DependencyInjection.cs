using GymErp.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Infrastructure.ServiceBus;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceBus(this IServiceCollection services)
    {
        services.AddScoped<IServiceBus, NoOpServiceBus>();
        return services;
    }
}
