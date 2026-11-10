using GymErp.Core.Ports.Driven;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Adapters.NoOpNotifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNoOpNotifications(this IServiceCollection services) =>
        services.AddSingleton<IForNotifyingEnrollmentEvents, NoOpEnrollmentEventNotifier>();
}
