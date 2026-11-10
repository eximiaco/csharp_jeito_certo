using CSharpFunctionalExtensions;
using Dapper;
using Npgsql;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;

namespace Eximia.Integrations.DispatchersTopics;

public class DispatchersTopicsDataAccess(PostgresConnectionStringFactory connectionStringFactory)
{
    public async Task<Maybe<DispatcherTopics>> GetFromTopicAsync(string topic)
    {
        var sql = $@"SELECT  Pod,
                             Topics,
                             Namespace
                     FROM dispatchers_topics
                     WHERE topics @> '[""{topic}""]'";

        await using NpgsqlConnection connection = new(connectionStringFactory.CreateConnectionString().ConnectionString);
        var result = await connection.QueryFirstOrDefaultAsync<DispatcherTopicsDto>(sql);
        if (result is null)
            return Maybe.None;
        return new DispatcherTopics(result.Pod, result.Topics.ToObject<IEnumerable<string>>(), result.Namespace);
    }

    public record DispatcherTopicsDto(string Pod, string Topics, string Namespace);
}
