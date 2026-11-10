using Eximia.Integrations.Shared;
using Eximia.Integrations.IntegrationEvents.Actions;

namespace Eximia.Integrations.IntegrationEvents.ValidationRules;

public class ValidateCallbackRule : IValidationRule
{
    private readonly IntegrationEventsDataAccess _integrationEventsDataAccess;

    public ValidateCallbackRule(IntegrationEventsDataAccess integrationEventsDataAccess)
    {
        _integrationEventsDataAccess = integrationEventsDataAccess;
    }
    
    public async Task<ResultAggregate> Validate(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if(!integrationEvent.Control.IsCallback)
            return ResultAggregate.Success();

        if(integrationEvent.Control.IsDev())
            return ResultAggregate.Success();
        
        var anyEvent = await _integrationEventsDataAccess.AnyEventWaitingCallback(integrationEvent, cancellationToken);
        return !anyEvent 
            ? ResultAggregate.Failure("isCallback", "Any integration event waiting callback") 
            : ResultAggregate.Success();
    }
}
