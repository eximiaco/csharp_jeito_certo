using System.Reflection;
using Hangfire;
using Serilog;
using Eximia.Integration.BackgroundAgent.Infrastructure;
using Eximia.Integration.BackgroundAgent.Infrastructure.HangfireFilters;
using Eximia.Integration.BackgroundAgent.Workers;
using Eximia.Integrations.PlatformEvents;
using Eximia.Integrations;
using Eximia.Integrations.Routing;

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
        .AddEndpointsApiExplorer()
        .AddMemoryCache()
        .AddCustomCors()
        .AddHealth(builder.Configuration)
        .AddCustomOptions(builder.Configuration)
        // PROBLEMA: o agent não recebe HTTP e carrega Hangfire, Kestrel, MVC e dashboard. Dois jobs simples não precisam disso.
        .AddHangfireJobs(builder.Configuration)
        .AddOptions()
        .AddKafka(builder.Configuration)
        .AddEfDbContext(builder.Configuration)
        // Registro explícito: só o que este host usa.
        .AddIntegrationsShared()
        .AddScoped<RoutingDataAccess>()
        .AddScoped<ReprocessStuckEvents>()
        .AddHostedService<ReprocessStuckEventsWorker>()
        .AddScoped<PlatformEventTimelineDataAccess>()
        .AddSingleton<PlatformEventsBus>()
        .AddHostedService<IntegrationTimelineWorker>()
        .AddHostedService<IntegrationErrorAlertWorker>()
        // PROBLEMA: MVC sem nenhum controller.
        .AddControllers();

    builder.Host.UseSerilog();
    // Container nativo com validação LIGADA em todos os ambientes: captive dependency e dependência não
    // registrada viram erro na subida, e não na primeira execução.
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    // Prazo de encerramento explícito: uma iteração em andamento precisa caber aqui, e ele deve ser MENOR que
    // o terminationGracePeriodSeconds do pod.
    builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));

    var app = builder.Build();
    app.UseHealthChecks("/health-ready");
    app.UseHealthChecks("/health-check");
    // PROBLEMA: dashboard exposto num serviço que deveria ser só um Worker.
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new AllowAllDashboardAuthorizationFilter()],
        DarkModeEnabled = true,
        IgnoreAntiforgeryToken = true,
        DashboardTitle = "Jobs Plataforma Eximia",
        AppPath = "/background-agent", // Define o caminho para o link "Back to site"
        PrefixPath = "/background-agent" // Ajusta os caminhos dos recursos estáticos        
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.MapControllers();
    app.MapHangfireDashboard("/hangfire").RequireAuthorization("HangfirePolicy");
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
