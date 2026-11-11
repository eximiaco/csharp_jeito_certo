using System.Reflection;
using Serilog;
using Eximia.Integration.BackgroundAgent.Infrastructure;
using Eximia.Integration.BackgroundAgent.Workers;
using Eximia.Integration.BackgroundAgent.PlatformEvents;
using Eximia.Integrations.PlatformEvents;
using Eximia.Integrations;

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
        // Registro explícito: só o que este host usa.
        .AddIntegrationsShared()
        .AddScoped<Eximia.Integrations.Routing.RoutingDataAccess>()
        .AddScoped<ReprocessStuckEvents>()
        .AddScoped<PlatformEventTimelineDataAccess>()
        .AddScoped<IPlatformEventHandler, TimelineHandler>()
        .AddScoped<IPlatformEventHandler, ErrorAlertHandler>()
        .AddHostedService<PlatformEventsConsumer>()
        .AddHostedService<ReprocessStuckEventsWorker>();

    builder.Services.AddSerilog();

    // Container nativo com validação LIGADA em todos os ambientes: captive dependency e dependência não
    // registrada viram erro na subida, e não na primeira execução.
    builder.ConfigureContainer(new DefaultServiceProviderFactory(
        new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));

    // Prazo de encerramento explícito: uma iteração em andamento precisa caber aqui, e ele deve ser MENOR que
    // o terminationGracePeriodSeconds do pod.
    builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));

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
