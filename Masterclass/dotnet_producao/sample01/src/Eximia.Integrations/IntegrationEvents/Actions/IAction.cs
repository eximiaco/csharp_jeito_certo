using CSharpFunctionalExtensions;

namespace Eximia.Integrations.IntegrationEvents.Actions;

public interface IAction
{
    public Task<Result> Execute(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}