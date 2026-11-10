using Microsoft.Extensions.DependencyInjection;
using Eximia.Integrations.Shared;
using Eximia.Integrations.IntegrationEvents;
using ILogger = Serilog.ILogger;
using Eximia.Integrations.IntegrationEvents.Actions;

namespace Eximia.Integrations.DomainServices;

public class IntegrationEventActionsEngine
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public IntegrationEventActionsEngine(
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var policy = integrationEvent.GetActionsForPosRegister();
        if (policy.IsEmpty())
            return;
        
        foreach (var actionType in policy.Actions)
        {
            if (_serviceProvider.GetRequiredService(actionType) is IAction rule)
            {
                var actionResult = await rule.Execute(integrationEvent, cancellationToken);
                if (!actionResult.IsFailure) 
                    continue;
                
                _logger
                    .ForContext("pos-action-error", actionResult.Error)
                    .Error("Post action error on integration event [{@integration}]", integrationEvent.Id);
            }
            else
            {
                throw new ArgumentException(@"Action invalid", actionType.FullName);
            }
        }
    }
}
