using Eximia.Integrations.IntegrationEvents.ValidationRules;

namespace Eximia.Integrations.IntegrationEvents.Actions;

public class ValidationPolicy
{
    private List<Type> _rulesTypes;

    public ValidationPolicy()
    {
        _rulesTypes = new List<Type>();
    }

    public IEnumerable<Type> Rules => _rulesTypes.AsEnumerable();

    public ValidationPolicy AddRule(Type type)
    {
        _rulesTypes.Add(type);
        return this;
    }

    public bool IsEmpty() => !_rulesTypes.Any();

    public static ValidationPolicy CreateCallbackPolicy()
        => new ValidationPolicy()
            .AddRule(typeof(ValidateCallbackRule));

    public static ValidationPolicy CreateEmpty()
        => new ValidationPolicy();
}