using System.Reflection;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Routing.Exceptions;

namespace Eximia.Integrations.Routing.Dispatchers;

public class DestinationDispatcherFactory
{
    private readonly IServiceProvider _serviceProvider;
    private Type[] _assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();

    public DestinationDispatcherFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IDestinationDispatcher CreateService(Route route)
    {
        var dispatcherType = Type.GetType(route.ClassName)?? _assemblyTypes.FirstOrDefault(c => c.Name == route.ClassName);
        if (dispatcherType == null)
        {
            throw new FailureDispatcherException(
                route.Destination, 
                route.Entity, 
                0,
                $"Invalid dispatcher type ({route.ClassName})");
        }

        if (_serviceProvider.GetService(dispatcherType) is not IDestinationDispatcher service)
            throw new FailureDispatcherException(
                route.Destination, 
                route.Entity, 
                0,
                $"None dispatcher has found for ({route.ClassName})");
        return service;
    }
    
}
