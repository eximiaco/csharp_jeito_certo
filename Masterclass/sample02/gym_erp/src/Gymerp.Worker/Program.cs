using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Gymerp.Worker.Services;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Repositories;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Services;
using Gymerp.Infrastructure.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<AttendanceCheckWorker>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IAccessRecordRepository, AccessRecordRepository>();
        services.AddScoped<IScheduledClassRepository, ScheduledClassRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAttendanceCheckService, AttendanceCheckService>();

        // Configuração do HttpClient para Pagar.me
        services.AddHttpClient("PagarMe", client =>
        {
            client.BaseAddress = new Uri(hostContext.Configuration["PagarMe:ApiUrl"]);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
    })
    .Build();

await host.RunAsync();
