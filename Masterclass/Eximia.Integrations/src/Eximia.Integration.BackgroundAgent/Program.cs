using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Serilog;
using Eximia.Integration.BackgroundAgent.Infrastructure;
using Eximia.Integration.BackgroundAgent.Infrastructure.HangfireFilters;
using Eximia.Integration.BackgroundAgent.Workers;

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
        .AddHangfireJobs(builder.Configuration)
        .AddOptions()
        .AddKafka(builder.Configuration)
        .AddEfDbContext(builder.Configuration)
        .AddHostedService<ReprocessStuckEventsWorker>()
        .AddControllers();

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.UseSerilog();
    builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new ApplicationModule());
    });

    var app = builder.Build();
    app.UseHealthChecks("/health-ready");
    app.UseHealthChecks("/health-check");
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
