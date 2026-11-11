using System.Reflection;
using Serilog;
using Eximia.Integration.BackgroundAgent.Infrastructure;
using Eximia.Integration.BackgroundAgent.Workers;
using Eximia.Integration.BackgroundAgent.PlatformEvents;
using Eximia.Integrations.PlatformEvents;
using Eximia.Integrations;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared.Postgres;

// Worker Service puro: este serviço não recebe HTTP, então não há Web API aqui.
var builder = Host.CreateApplicationBuilder(args);
var assemblyName = Assembly.GetExecutingAssembly().GetName();
var serviceName = assemblyName.Name;

try
{
    Log.ForContext("ApplicationName", serviceName).Information("Starting application");

    builder.Services
        .AddLogs(builder.Configuration)
        .AddMemoryCache()
        .AddCustomOptions(builder.Configuration)
        .AddOptions()
        .AddKafka(builder.Configuration)
        .AddEfDbContext(builder.Configuration)
        .AddIntegrationsShared()
        .AddScoped<RoutingDataAccess>()
        .AddSingleton<PostgresAdvisoryLock>()
        .AddScoped<PlatformEventTimelineDataAccess>()
        .AddScoped<IPlatformEventHandler, TimelineHandler>()
        .AddScoped<IPlatformEventHandler, ErrorAlertHandler>()
        .AddHostedService<PlatformEventsConsumer>()
        .AddHostedService<ReprocessStuckEventsWorker>();

    builder.Services.AddSerilog();

    builder.ConfigureContainer(new DefaultServiceProviderFactory(
        // PROBLEMA: validação do container desligada. Captive dependency e dependência não registrada passam em silêncio.
        new ServiceProviderOptions { ValidateScopes = false, ValidateOnBuild = false }));

    // PROBLEMA: sem HostOptions.ShutdownTimeout explícito. Os retries do destino podem passar do padrão (30 s) e levar SIGKILL.
    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.ForContext("ApplicationName", serviceName)
        .Fatal(ex, "Program terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
