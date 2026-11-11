using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Eximia.Integrations.Auth;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.DestinationsErrorsConfiguration.DataAccess;
using Eximia.Integrations.DomainServices;
using Eximia.Integrations.Events;
using Eximia.Integrations.Files.Services;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.IntegrationEvents.Actions;
using Eximia.Integrations.IntegrationEvents.Application;
using Eximia.Integrations.IntegrationEvents.Application.Register;
using Eximia.Integrations.IntegrationEvents.Application.Routing;
using Eximia.Integrations.IntegrationEvents.Services;
using Eximia.Integrations.Outbox;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Routing.Authentication.Providers;
using Eximia.Integrations.Routing.Dispatchers;
using Eximia.Integrations.Routing.Rules.Engine;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;

namespace Eximia.Integrations;

/// <summary>
///     Registro EXPLÍCITO das dependências. Nada de varredura de assembly: quem lê esta classe sabe o que existe,
///     com qual tempo de vida e por quê.
///
///     Regra de ouro: singleton só para o que não tem estado por mensagem e é thread-safe; tudo que toca banco
///     ou carrega estado de uma operação é scoped (o Silverback cria um escopo por mensagem consumida).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Acesso ao banco e eventos da plataforma: o mínimo que qualquer host precisa.</summary>
    public static IServiceCollection AddIntegrationsShared(this IServiceCollection services)
    {
        // Singletons: sem estado por mensagem, thread-safe. Quem tem Dispose é descartado pelo container.
        services.AddSingleton<PostgresConnectionStringFactory>();

        // DONO do pool de conexões com o Postgres. Por estar no container, ele é descartado no encerramento do
        // host, e as conexões ociosas são fechadas pelo protocolo do Postgres, e não cortadas quando o processo morre.
        services.AddSingleton(sp => NpgsqlDataSource.Create(
            sp.GetRequiredService<PostgresConnectionStringFactory>().CreateConnectionString()));
        services.AddSingleton<PostgresAdvisoryLock>();

        services.AddScoped<IntegrationEventsDataAccess>();
        services.AddScoped<PublishPlataformEventService>();
        return services;
    }

    /// <summary>Roteamento, autenticação no destino e storage do payload.</summary>
    public static IServiceCollection AddIntegrationsRouting(this IServiceCollection services)
    {
        services.AddScoped<RoutingDataAccess>();
        services.AddScoped<RoutingService>();
        services.AddScoped<RoutingRuleEngine>();

        // O dispatcher é escolhido em tempo de execução pelo nome da classe gravado na rota (banco).
        // Por isso a fábrica resolve pelo IServiceProvider: é um ponto de extensão deliberado, e todo
        // dispatcher possível precisa estar registrado aqui.
        services.AddScoped<DestinationDispatcherFactory>();
        services.AddScoped<HttpJsonDispatcher>();
        services.AddScoped<HttpJsonWithJwtDispatcher>();

        services.AddScoped<ClientCredentialsProvider>();

        services.AddScoped<IDestinationErrorConfigurationDataAccess, DestinationErrorConfigurationDataAccess>();
        services.AddScoped<CheckIfErrorStatusCodeIsAllowedService>();

        services.AddScoped<UploadFileService>();
        services.AddScoped<PayloadService>();
        return services;
    }

    /// <summary>Autenticação da plataforma na própria API (usada para devolver callbacks de erro).</summary>
    public static IServiceCollection AddIntegrationsAuth(this IServiceCollection services)
    {
        services.AddScoped<AzureAdAuthenticationService>();
        services.AddScoped<AzureAdAuthenticationDecorator>();
        return services;
    }

    /// <summary>Caso de uso: registrar evento (e callback) vindo das origens, usado pela API.</summary>
    public static IServiceCollection AddRegisterIntegrationEvent(this IServiceCollection services)
    {
        services.AddScoped<RegisterMultipleRoutesIntegrationEventApplication>();
        services.AddScoped<JsonSchemaService>();

        // As ações pós-registro são resolvidas por tipo pelo engine (IServiceProvider.GetRequiredService),
        // então precisam estar registradas aqui.
        services.AddScoped<IntegrationEventActionsEngine>();
        services.AddScoped<CallbackFinalizeOriginalIntegrationEvent>();
        return services;
    }

    /// <summary>Caso de uso: rotear o evento ao destino, usado pelo consumidor.</summary>
    public static IServiceCollection AddRouteIntegrationEvent(this IServiceCollection services)
    {
        services.AddScoped<RouteIntegrationEventApplication>();
        services.AddScoped<HandleRoutingErrorService>();
        // O consumer (subscriber do Silverback) é registrado no host, com AddScopedSubscriber.
        return services;
    }

    /// <summary>Caso de uso: devolver o callback de erro à origem, usado pelo consumidor.</summary>
    public static IServiceCollection AddProcessIntegrationEventCallback(this IServiceCollection services)
    {
        services.AddScoped<SendIntegrationEventService>();
        // O handler (subscriber do Silverback) é registrado no host, com AddScopedSubscriber.
        return services;
    }
}
