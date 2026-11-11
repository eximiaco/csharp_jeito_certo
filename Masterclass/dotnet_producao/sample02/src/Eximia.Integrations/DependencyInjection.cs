using Microsoft.Extensions.DependencyInjection;
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
using Eximia.Integrations.Routing;
using Eximia.Integrations.Routing.Authentication;
using Eximia.Integrations.Routing.Authentication.Providers;
using Eximia.Integrations.Routing.Dispatchers;
using Eximia.Integrations.Routing.Rules.Engine;
using Eximia.Integrations.Shared.Postgres;

namespace Eximia.Integrations;

/// <summary>Registro das dependências da biblioteca, agrupado por caso de uso. Cada host chama só o que usa.</summary>
public static class DependencyInjection
{
    /// <summary>Acesso ao banco e eventos da plataforma: o mínimo que qualquer host precisa.</summary>
    public static IServiceCollection AddIntegrationsShared(this IServiceCollection services)
    {
        services.AddSingleton<PostgresConnectionStringFactory>();
        // PROBLEMA: singleton que depende de serviço scoped (captive dependency):
        // o IPublisher é scoped e passa a viver o tempo do processo.
        services.AddSingleton<PublishPlataformEventService>();
        services.AddScoped<IntegrationEventsDataAccess>();
        return services;
    }

    /// <summary>Roteamento, autenticação no destino e storage do payload.</summary>
    public static IServiceCollection AddIntegrationsRouting(this IServiceCollection services)
    {
        services.AddScoped<RoutingDataAccess>();
        services.AddScoped<RoutingService>();
        services.AddScoped<RoutingRuleEngine>();

        // O dispatcher é escolhido em tempo de execução pelo nome da classe gravado na rota (banco).
        services.AddScoped<DestinationDispatcherFactory>();
        services.AddScoped<HttpJsonDispatcher>();
        services.AddScoped<HttpJsonWithJwtDispatcher>();

        // O cache de tokens PRECISA ser singleton: os semáforos single-flight vivem nele.
        services.AddSingleton<TokenCache>();
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
        // Usado em toda requisição da API: uma instância só.
        // PROBLEMA: singleton que depende de serviço scoped (captive dependency):
        // todas as requisições compartilham o mesmo DbContext (que não é thread-safe).
        services.AddSingleton<RegisterMultipleRoutesIntegrationEventApplication>();
        services.AddScoped<JsonSchemaService>();

        // As ações pós-registro são resolvidas por tipo pelo engine (IServiceProvider.GetRequiredService).
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
        // Só lê uma configuração e faz um POST: uma instância basta para todo o processo.
        // PROBLEMA: singleton desnecessário. Nada é compartilhado: é uma leitura de configuração e um POST.
        // Não quebra hoje; a próxima dependência scoped injetada aqui vira captive dependency sem aviso.
        services.AddSingleton<SendIntegrationEventService>();
        // O handler (subscriber do Silverback) é registrado no host, com AddScopedSubscriber.
        return services;
    }
}
