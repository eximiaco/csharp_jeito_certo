using CSharpFunctionalExtensions;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.DestinationsErrorsConfiguration.DataAccess;

public interface IDestinationErrorConfigurationDataAccess
{
    Task<Maybe<DestinationErrorConfiguration>> GetAsync(string id, CancellationToken cancellationToken = default);
}
