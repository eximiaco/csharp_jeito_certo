using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Eximia.Integrations.HttpApi.Infrastructure;
using Eximia.Integrations.HttpApi.Infrastructure.AutofacModules;

var builder = WebApplication.CreateBuilder(args);
var assemblyName = Assembly.GetExecutingAssembly().GetName();
var serviceName = assemblyName.Name;
var serviceVersion = Environment.GetEnvironmentVariable("DD_VERSION") ?? 
                     Assembly.GetExecutingAssembly().GetName().Version?.ToString();

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
        .AddTelemetry(serviceName!, serviceVersion!, builder.Configuration)
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
        .AddControllers(o =>
        {
            o.InputFormatters.Insert(0, new JsonNodeInputFormatter(Log.Logger));
            o.Filters.Add(typeof(HttpGlobalExceptionFilter));
        });
    
    builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new ApplicationModule());
    });
    builder.Host.UseSerilog();
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    
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
