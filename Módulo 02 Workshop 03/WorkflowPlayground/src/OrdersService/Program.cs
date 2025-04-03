using Microsoft.EntityFrameworkCore;
using Serilog;
using WorkflowCore.Interface;
using OrderService.Infra;
using OrderService.Infra.Services;
using OrderService.Workflow;
using OrderService.Workflow.Steps;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();
builder.Services.AddSerilog();

builder.Services.AddDbContext<PlaygroundDbContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("Database"));
});

builder.Services.AddWorkflow(x =>
{
    x.UseSqlServer(builder.Configuration.GetConnectionString("WorkflowDatabase"), true, true);
    x.UseMaxConcurrentWorkflows(2);
});

builder.Services.AddScoped<ReserveStockStep>();
builder.Services.AddScoped<ProcessPaymentStep>();
builder.Services.AddScoped<ScheduleShippingStep>();
builder.Services.AddScoped<FinishOrderStep>();
builder.Services.AddScoped<UndoProcessingStep>();

builder.Services.AddHttpClient<PaymentService>(x => x.BaseAddress = new Uri("http://localhost:5243"));
builder.Services.AddHttpClient<StockService>(x => x.BaseAddress = new Uri("http://localhost:5220"));
builder.Services.AddHttpClient<ShippingService>(x => x.BaseAddress = new Uri("http://localhost:5178"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();

app.Services.GetRequiredService<IWorkflowHost>().RegisterWorkflow<OrderWorkflowDefinition, OrderWorflowData>();
app.Services.GetRequiredService<IWorkflowHost>().Start();

using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<PlaygroundDbContext>().Database.MigrateAsync().Wait();

app.MapControllers();

await app.RunAsync();
