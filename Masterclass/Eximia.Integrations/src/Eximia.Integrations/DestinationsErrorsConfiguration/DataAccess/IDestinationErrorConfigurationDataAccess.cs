using CSharpFunctionalExtensions;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.DestinationsErrorsConfiguration.DataAccess;

public interface IDestinationErrorConfigurationDataAccess : IService<IDestinationErrorConfigurationDataAccess>
{
    Task<Maybe<DestinationErrorConfiguration>> GetAsync(string id, CancellationToken cancellationToken = default);
}
