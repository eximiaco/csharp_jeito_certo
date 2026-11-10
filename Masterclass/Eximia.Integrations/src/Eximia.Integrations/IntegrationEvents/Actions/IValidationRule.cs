using Eximia.Integrations.Shared;

namespace Eximia.Integrations.IntegrationEvents.Actions;

public interface IValidationRule
{
    Task<ResultAggregate> Validate(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}