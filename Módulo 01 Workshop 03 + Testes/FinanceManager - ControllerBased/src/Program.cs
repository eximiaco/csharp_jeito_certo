using FinanceManager.Infra;
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

var connectionString = builder.Configuration.GetConnectionString("Database");

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
    .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>())
    .AddHostedService<CreateAdminUserHostedService>()
    .AddDatabase(builder.Configuration)
    .AddResiliencePolicies()
    .AddServices(builder.Configuration)
    .AddControllers();

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString!, tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

public partial class Program
{
}
