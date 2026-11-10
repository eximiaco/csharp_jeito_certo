using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Eximia.Integrations.Shared.Postgres;
using ILogger = Serilog.ILogger;

namespace Eximia.Integrations.Outbox;

public static class EximiaOutboxReaderServiceCollectionExtensions
{
    /// <summary>
    ///     Faz o <see cref="OutboxWorker" /> padrão do Silverback ler o outbox PostgreSql através do
    ///     <see cref="EximiaOutboxReader" /> (status InQueue + métricas). Deve ser chamado depois de
    ///     <c>AddPostgreSqlOutbox()</c>.
    /// </summary>
    public static IServiceCollection UseEximiaOutboxReader(this IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(OutboxReaderFactory));
        if (descriptor?.ImplementationInstance is not OutboxReaderFactory readerFactory)
            throw new InvalidOperationException(
                "OutboxReaderFactory not found. Call AddPostgreSqlOutbox() before UseEximiaOutboxReader().");

        // A fábrica padrão não permite registrar duas vezes o mesmo tipo de settings, então substituímos o
        // resolvedor (somente o storage PostgreSql é usado por este sistema).
        readerFactory.OverrideFactories((settings, serviceProvider) =>
        {
            if (settings is not PostgreSqlOutboxSettings postgreSqlSettings)
                throw new InvalidOperationException(
                    $"Unsupported outbox settings type '{settings.GetType().FullName}'. Only PostgreSql is supported.");

            return new EximiaOutboxReader(
                new PostgreSqlOutboxReader(postgreSqlSettings),
                serviceProvider.GetRequiredService<PostgresConnectionStringFactory>(),
                serviceProvider.GetRequiredService<ILogger>());
        });

        return services;
    }
}
