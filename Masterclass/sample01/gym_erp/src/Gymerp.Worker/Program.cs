using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Services;
using Gymerp.Worker.Services;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Repositories;
using Gymerp.Infrastructure.Services;

namespace Gymerp.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<AttendanceCheckWorker>();
                    services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
                    services.AddScoped<IAccessRecordRepository, AccessRecordRepository>();
                    services.AddScoped<IScheduledClassRepository, ScheduledClassRepository>();
                    services.AddScoped<IPaymentRepository, PaymentRepository>();
                    services.AddScoped<IPaymentService, PaymentService>();
                    services.AddScoped<INotificationService, NotificationService>();
                    services.AddScoped<IAttendanceCheckService, AttendanceCheckService>();
                });
    }
}
