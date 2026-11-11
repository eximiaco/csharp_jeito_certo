namespace Eximia.Integrations.IntegrationEvents.Actions;

public class PosRegisterIntegrationEventActions
{
    private List<Type> _actionsTypes;

    public PosRegisterIntegrationEventActions()
    {
        _actionsTypes = new List<Type>();
    }

    public IEnumerable<Type> Actions => _actionsTypes.AsEnumerable();

    public PosRegisterIntegrationEventActions Add(Type type)
    {
        _actionsTypes.Add(type);
        return this;
    }

    public bool IsEmpty() => !_actionsTypes.Any();

    public static PosRegisterIntegrationEventActions CreateCallbackPolicy()
        => new PosRegisterIntegrationEventActions()
            .Add(typeof(CallbackFinalizeOriginalIntegrationEvent));

    public static PosRegisterIntegrationEventActions CreateEmpty()
        => new PosRegisterIntegrationEventActions();
}