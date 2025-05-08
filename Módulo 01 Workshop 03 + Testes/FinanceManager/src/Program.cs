using FinanceManager.BankAccounts.BackgroundServices;
using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using FinanceManager.Crosscutting.Healthchecks;
using FinanceManager.Crosscutting.HostedServices;
using FinanceManager.Crosscutting.Middlewares;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Database connection string is missing");

builder.Host.UseSerilog();

builder.Services.AddApiVersioning(cfg => { cfg.ReportApiVersions = true; });
builder.Services
    .AddHealthChecksUI()
    .AddInMemoryStorage();

builder.Services
    .AddLogging(builder.Configuration)
    .AddAuthorizationAndAuthentication(builder.Configuration)
    .AddEndpointsApiExplorer()
    .AddHostedService<CreateAdminUserHostedService>()
    .AddHostedService<SyncUsdValueConsumerBackgroundService>()
    .AddHostedService<SyncUsdValueFallback>()
    .AddDatabase(builder.Configuration)
    .AddResiliencePolicies()
    .AddServices(builder.Configuration)
    .AddOpenApi()
    .AddHealthChecks()
        .AddCheck<ResourceCheckerHealthCkeck>("Metrics")
        .AddSqlServer(
            connectionString: connectionString!,
            name: "SqlServer",
            timeout: TimeSpan.FromSeconds(10),
            tags: ["ready"]);

builder.Services.AddSingleton(_ => Channel.CreateUnbounded<string>());

var app = builder.Build();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
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

app.MapHealthChecksUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<LogScopedMiddleware>();
app.UseMiddleware<GlobalErrorHandlingMiddleware>();

app.UseEndpoints();

using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>().Database.MigrateAsync();
await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();

await app.RunAsync();

public partial class Program { }
