using Dapper;
using Npgsql;

namespace Eximia.Integrations.Shared.Postgres;

/// <summary>
///     Exclusão mútua entre réplicas usando advisory lock do Postgres: só UMA réplica executa o trabalho por vez.
///     É o que um scheduler com banco (como o Hangfire) fornecia "de graça" e que um Worker puro precisa fazer
///     explicitamente.
/// </summary>
public sealed class PostgresAdvisoryLock(NpgsqlDataSource dataSource)
{
    /// <returns>O lock (descarte para liberar), ou <c>null</c> se outra réplica já o detém.</returns>
    public async Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken)
    {
        // O lock vale para a SESSÃO: a conexão precisa ficar aberta enquanto o trabalho roda.
        var connection = dataSource.CreateConnection();
        try
        {
            await connection.OpenAsync(cancellationToken);
            var acquired = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT pg_try_advisory_lock(hashtext(@name))", new { name }, cancellationToken: cancellationToken));

            if (acquired)
                return new Lease(connection, name);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        await connection.DisposeAsync();
        return null;
    }

    private sealed class Lease(NpgsqlConnection connection, string name) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                // Liberar NÃO pode depender do token já cancelado (shutdown).
                await connection.ExecuteAsync(new CommandDefinition(
                    "SELECT pg_advisory_unlock(hashtext(@name))", new { name }, cancellationToken: CancellationToken.None));
            }
            catch
            {
                // A conexão será descartada de qualquer forma; o Postgres libera o lock da sessão.
            }

            await connection.DisposeAsync();
        }
    }
}
