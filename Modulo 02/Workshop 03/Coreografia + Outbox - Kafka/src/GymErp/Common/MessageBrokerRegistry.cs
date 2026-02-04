using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Common;

/// <summary>
/// Registry of message broker extensions. Each broker registers itself (config + DI) so that
/// adding a new broker only requires a new extension and no change to Program or AddMessageBroker.
/// </summary>
public static class MessageBrokerRegistry
{
    private static readonly Dictionary<string, Action<IServiceCollection, IConfiguration>> _registrations = new();

    public static void Register(string name, Action<IServiceCollection, IConfiguration> configure)
    {
        _registrations[name] = configure;
    }

    public static void Invoke(string name, IServiceCollection services, IConfiguration configuration)
    {
        if (!_registrations.TryGetValue(name, out var configure))
            throw new InvalidOperationException($"Message broker '{name}' is not registered. Registered: {string.Join(", ", _registrations.Keys)}.");
        configure(services, configuration);
    }
}
