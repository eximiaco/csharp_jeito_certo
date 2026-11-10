using GymErp.Core.Ports.Driven;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Adapters.InMemory;

public static class DependencyInjection
{
    // Singleton: os dados vivem enquanto o processo vive.
    public static IServiceCollection AddInMemoryStorage(this IServiceCollection services) =>
        services.AddSingleton<IForStoringEnrollments, InMemoryEnrollments>();
}
