using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Eximia.Integrations.HttpApi.Infrastructure;
using Eximia.Integrations;

var builder = WebApplication.CreateBuilder(args);
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

    Log.ForContext("ApplicationName", serviceName).Information("Starting application");
    builder.Services
        .AddLogs(builder.Configuration)
        .AddKafka(builder.Configuration)
        .AddEndpointsApiExplorer()
        .AddSwaggerDoc()
        .AddCustomCors()
        .AddSecurity(builder.Configuration)
        .AddHealth(builder.Configuration)
        .AddOptions()
        .AddCustomOptions(builder.Configuration)
        .AddCaching()
        .AddHttpContextAccessor()
        .AddEfDbContext(builder.Configuration)
        .AddGoogleStorageAuth(builder.Configuration)
        .AddSingleton<EximiaProblemDetailsFactory>()
        // Registro explícito: infraestrutura compartilhada + o caso de uso que este host executa.
        .AddIntegrationsShared()
        .AddIntegrationsRouting()
        .AddRegisterIntegrationEvent()
        .AddControllers(o =>
        {
            o.InputFormatters.Insert(0, new JsonNodeInputFormatter(Log.Logger));
            o.Filters.Add(typeof(HttpGlobalExceptionFilter));
        });
    
    builder.Host.UseSerilog();
    // Container nativo com validação LIGADA em todos os ambientes: captive dependency e dependência não
    // registrada viram erro na subida, e não na primeira requisição.
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    // Prazo de encerramento explícito: requisições em andamento e o OutboxWorker precisam terminar dentro dele, e deve ser MENOR que o terminationGracePeriodSeconds do pod
    // (senão o orquestrador mata o processo antes de o host terminar de parar os serviços).
    builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));
    
    var app = builder.Build();

    // Tabela do outbox padrão do Silverback (CREATE TABLE IF NOT EXISTS).
    await app.Services.EnsureOutboxTableAsync(builder.Configuration);

    app.MapHealthChecks("/health-ready", new HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health-check", new HealthCheckOptions
    {
        Predicate = (check) => check.Tags.Contains("kafka-healthy")
    });

    app.UseCors("default");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
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
