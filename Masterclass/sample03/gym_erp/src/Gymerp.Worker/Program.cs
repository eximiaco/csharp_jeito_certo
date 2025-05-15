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
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAttendanceCheckService, AttendanceService>();
    })
    .Build();

await host.RunAsync();
