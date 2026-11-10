using Microsoft.Extensions.DependencyInjection;
using Eximia.Integrations.Shared;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.IntegrationEvents.Actions;

namespace Eximia.Integrations.DomainServices;

public class IntegrationEventPolicyEngine : IService<IntegrationEventPolicyEngine>
{
    private readonly IServiceProvider _serviceProvider;

    public IntegrationEventPolicyEngine(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ResultAggregate> Execute(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var policy = integrationEvent.GetValidationPolicy();
        if (policy.IsEmpty())
            return ResultAggregate.Success();

        var result = ResultAggregate.Success();
        foreach (var ruleType in policy.Rules)
        {
            if (_serviceProvider.GetRequiredService(ruleType) is IValidationRule rule)
            {
                var ruleResult = await rule.Validate(integrationEvent, cancellationToken);
                result = ResultAggregate.Combine(result, ruleResult);
            }
            else
                throw new ArgumentException("Validation Rule invalid", ruleType.FullName);
        }
        return result;
    }
}
