using CSharpFunctionalExtensions;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.IntegrationEvents.Actions;

public class CallbackFinalizeOriginalIntegrationEvent : IAction
{
    private readonly IntegrationEventsDataAccess _integrationEventsDataAccess;

    public CallbackFinalizeOriginalIntegrationEvent(IntegrationEventsDataAccess integrationEventsDataAccess)
    {
        _integrationEventsDataAccess = integrationEventsDataAccess;
    }
    
    public async Task<Result> Execute(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        try
        {
            await _integrationEventsDataAccess.FinalizeOriginalIntegrationEventsForCallback(integrationEvent,
                cancellationToken);
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(e.Message);
        }
    }
}
