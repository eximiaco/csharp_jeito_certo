using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gymerp.Application.Interfaces;

namespace Gymerp.Worker.Services
{
    public class AttendanceCheckWorker : BackgroundService
    {
        private readonly ILogger<AttendanceCheckWorker> _logger;
        private readonly IAttendanceCheckService _attendanceCheckService;

        public AttendanceCheckWorker(
            ILogger<AttendanceCheckWorker> logger,
            IAttendanceCheckService attendanceCheckService)
        {
            _logger = logger;
            _attendanceCheckService = attendanceCheckService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Verificando agendamentos de aulas sem registro de acesso...");
                await _attendanceCheckService.CheckAttendanceAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
} 