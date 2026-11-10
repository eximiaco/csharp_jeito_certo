using CSharpFunctionalExtensions;
using Dapper;
using Npgsql;
using System.Data;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.DestinationsErrorsConfiguration.Strategies;

namespace Eximia.Integrations.DestinationsErrorsConfiguration.DataAccess;

public class DestinationErrorConfigurationDataAccess : IDestinationErrorConfigurationDataAccess
{
    private readonly NpgsqlDataSource _dataSource;

    public DestinationErrorConfigurationDataAccess(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Maybe<DestinationErrorConfiguration>> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var sql = @"SELECT	destination_errors_allowed.id AS Id,
		                    destination_errors_allowed.error_status_codes_allowed AS ErrorStatusCodesAllowed,
		                    destination_errors_exception.status_code AS StatusCode,
		                    destination_errors_exception.response_strategy AS ResponseStrategy
                    FROM destination_errors_allowed
                    LEFT JOIN destination_errors_exception ON destination_errors_exception.destination_errors_allowed_id = destination_errors_allowed.id
                    WHERE destination_errors_allowed.id = @id";

        DynamicParameters param = new();
        param.Add("id", id, DbType.AnsiStringFixedLength, ParameterDirection.Input, 100);

        await using var connection = _dataSource.CreateConnection();
        var result = await connection.QueryAsync<DestinationErrorConfigurationDto>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (!result.Any())
            return Maybe.None;

        var destinationErrorsAllowed = result.First();
        return new DestinationErrorConfiguration(
            destinationErrorsAllowed.Id,
            statusCodeAllowed: string.IsNullOrWhiteSpace(destinationErrorsAllowed.ErrorStatusCodesAllowed)
                ? Enumerable.Empty<int>()
                : destinationErrorsAllowed.ErrorStatusCodesAllowed.ToObject<IEnumerable<int>>(),
            errorExceptions: result.Select(r => new DestinationErrorExceptionConfiguration(
                r.StatusCode,
                ResponseErrorStrategy: string.IsNullOrWhiteSpace(r.ResponseStrategy)
                    ? null!
                    : r.ResponseStrategy.ToNameTypeObject<IResponseErrorStrategy>())));
    }

    private record DestinationErrorConfigurationDto(string Id, string ErrorStatusCodesAllowed, int StatusCode, string ResponseStrategy);
}
