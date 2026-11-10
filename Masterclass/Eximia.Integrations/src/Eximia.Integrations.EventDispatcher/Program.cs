using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Eximia.Integrations;
using Eximia.Integrations.EventDispatcher.Infrastructure;

// Este serviço CONSOME mensagens: não tem controllers, Swagger nem CORS. O host web mínimo existe só para os
// probes de saúde que o Kubernetes precisa.
var builder = WebApplication.CreateSlimBuilder(args);
var assemblyName = Assembly.GetExecutingAssembly().GetName();
var serviceName = assemblyName.Name;

try
{
    builder
    .Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

    builder.Services.AddLogs(builder.Configuration);
    Log.ForContext("ApplicationName", serviceName).Information("Starting application");

    builder.Services
        .AddKafka(builder.Configuration)
        .AddHealth(builder.Configuration)
        .AddOptions()
        .AddCaching()
        .AddCustomOptions(builder.Configuration)
        .AddGoogleStorageAuth(builder.Configuration)
        // Registro explícito: infraestrutura compartilhada + os casos de uso que este host executa.
        .AddIntegrationsShared()
        .AddIntegrationsRouting()
        .AddIntegrationsAuth()
        .AddRouteIntegrationEvent()
        .AddProcessIntegrationEventCallback();

    builder.Host.UseSerilog();
    // Container nativo com validação LIGADA em todos os ambientes: captive dependency e dependência não
    // registrada viram erro na subida, e não na primeira mensagem.
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    // Prazo de encerramento explícito: precisa caber a entrega em andamento (retries do destino), e deve ser MENOR que o terminationGracePeriodSeconds do pod
    // (senão o orquestrador mata o processo antes de o host terminar de parar os serviços).
    builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(45));
    
    var app = builder.Build();

    app.MapHealthChecks("/health-ready", new HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health-check", new HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("kafka-healthy")
    });

    app.Run();
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
