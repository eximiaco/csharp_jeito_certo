using System.Reflection;
using FinanceManager.Infra;
using FinanceManager.Infra.Filter;
using FinanceManager.Infra.HostedServices;
using FinanceManager.Infra.Middlewares;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.OpenTelemetry;

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
        

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Console()
        .WriteTo.File("bankAccountLogs.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.OpenTelemetry(cfg =>
        {
            cfg.Endpoint = "http://localhost:5341/ingest/otlp/v1/logs";
            cfg.Protocol = OtlpProtocol.HttpProtobuf;
            cfg.Headers = new Dictionary<string, string>
            {
                ["X-Seq-ApiKey"] = "eScZ0yTcyN2oqAtNQ7dr"
            };
            cfg.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "FinanceManager"
            };
        })
        .WriteTo.Elasticsearch(
            new ElasticsearchSinkOptions(new Uri(builder.Configuration.GetConnectionString("LogElasticSource")!))
            {
                MinimumLogEventLevel = LogEventLevel.Information,
                IndexFormat = "finance-control-{0:yyyy.MM.dd}"
            })
        .CreateLogger();
    builder.Host.UseSerilog();

    builder.Services
        .AddVersioning()
        .AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration = builder.Configuration.GetConnectionString("Redis");
            opts.InstanceName = "FinanceManager";
        })
        .AddAuthorizationAndAuthentication(builder.Configuration)
        .AddEndpointsApiExplorer()
        .AddSwaggerGen()
        .AddHostedService<CleanupHostedService>()
        .AddHostedService<CheckBalanceBackgroundService>()
        .AddDatabase(builder.Configuration)
        .AddResiliencePolicies()
        .AddServices(builder.Configuration)
        .AddControllers(c =>
        {
            c.Filters.Add(typeof(GlobalExceptionHandler));
        });
    
    var connectionString = builder.Configuration.GetConnectionString("Database");
    builder.Services
        .AddHealthChecks()
        .AddSqlServer(connectionString!, tags: ["ready"]);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    /*
     *  UseHttpsRedirection() redireciona na hora (Muita gente desabilita UseHttpsRedirection() e configura o Nginx/Ingress para forçar HTTPS)
     *  UseHsts() manda o header Strict-Transport-Security, que faz o browser “memorizar” que aquele host só deve ser acessado via HTTPS (efeito mais forte, mas só faz sentido em produção e com cuidado)
     */
    //app.UseHttpsRedirection();
    //app.UseHsts();
    
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = registration => !registration.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.UseMiddleware<LogScopedMiddleware>();
    app.UseMiddleware<GlobalErrorHandlingMiddleware>();
    
    app.MapControllers();

    // app.Services.CreateScope().ServiceProvider.GetRequiredService<FinanceManagerDbContext>().Database.Migrate();
    // app.Services.CreateScope().ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();

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
