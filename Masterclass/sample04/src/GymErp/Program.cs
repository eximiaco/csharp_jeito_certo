using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastEndpoints;
using FastEndpoints.Swagger;
using GymErp.Bootstrap;
using Gymerp.Domain.Subscriptions.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

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
        .AddCors()
        .AddFastEndpoints()
        .AddLogs(builder.Configuration)
        .SwaggerDocument(c =>
        {
            //c.DocumentSettings = s => s.OperationProcessors.Add(new AddTenantHeaderSwagger());
        })
        .AddHttpContextAccessor()
        .AddHealth(builder.Configuration)
        .AddOptions()
        .AddCaching();
    
    builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new SubscriptionsModule());
    });
    builder.Host.UseSerilog();
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    
    var app = builder.Build();
    var basePath = builder.Configuration["BasePath"];
    app
        .UseCors(b => b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod())
        .UseHealthChecks("/healthz")
        .UseAuthentication()
        .UseAuthorization()
        //.UseTenantDbContext()
        .UseDefaultExceptionHandler()
        .UseFastEndpoints(config =>
        {
            if (!string.IsNullOrWhiteSpace(basePath)) config.Endpoints.RoutePrefix = basePath;
        })
        .UseSwaggerGen(config =>
            {
                if (!string.IsNullOrWhiteSpace(basePath)) config.Path = $"/{basePath}/{config.Path}";
            },
            settings =>
            {
                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    settings.Path = $"/{basePath}/swagger";
                    settings.DocumentPath = $"/{basePath}/{settings.DocumentPath}";
                }
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
