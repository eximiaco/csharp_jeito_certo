using GymErp.Adapters.InMemory;
using GymErp.Adapters.NoOpNotifications;
using GymErp.Adapters.Postgres;
using GymErp.Core;
using GymErp.Core.Ports.Driving;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = null; });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GymErp API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();

// ---------- Configurador: liga cada porta ao adaptador da vez ----------
// Porta de condução: implementada pelo próprio núcleo.
builder.Services.AddScoped<IForManagingEnrollments, EnrollmentManagement>();

// Porta conduzida de notificação.
builder.Services.AddNoOpNotifications();

// Porta conduzida de armazenamento: Postgres por padrão, ou em memória com "Storage": "InMemory"
// (por exemplo: dotnet run --Storage=InMemory). O núcleo não muda em nenhum dos dois casos.
if (string.Equals(builder.Configuration["Storage"], "InMemory", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddInMemoryStorage();
else
    builder.Services.AddPostgresStorage(builder.Configuration);

var app = builder.Build();

app.UseCors();
app.UseHealthChecks("/healthz");
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GymErp API v1"));

app.Run();
