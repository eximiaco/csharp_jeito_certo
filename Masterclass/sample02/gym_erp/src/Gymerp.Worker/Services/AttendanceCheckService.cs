using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gymerp.Domain.Interfaces;
using Gymerp.Domain.Entities;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Models;

namespace Gymerp.Worker.Services
{
    public class AttendanceCheckService : BackgroundService
    {
        private readonly ILogger<AttendanceCheckService> _logger;
        private readonly IAccessRecordRepository _accessRecordRepository;
        private readonly IScheduledClassRepository _scheduledClassRepository;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentRepository _paymentRepository;
        private const decimal FINE_AMOUNT = 10.00m;

        public AttendanceCheckService(
            ILogger<AttendanceCheckService> logger,
            IAccessRecordRepository accessRecordRepository,
            IScheduledClassRepository scheduledClassRepository,
            IPaymentService paymentService,
            IPaymentRepository paymentRepository)
        {
            _logger = logger;
            _accessRecordRepository = accessRecordRepository;
            _scheduledClassRepository = scheduledClassRepository;
            _paymentService = paymentService;
            _paymentRepository = paymentRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Verificando agendamentos de aulas sem registro de acesso...");
                await CheckAttendanceAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CheckAttendanceAsync()
        {
            var today = DateTime.UtcNow.Date;
            var scheduledClasses = await _scheduledClassRepository.GetByDateAsync(today);

            foreach (var scheduledClass in scheduledClasses)
            {
                var accessRecord = await _accessRecordRepository.GetByEnrollmentAndDateTimeAsync(
                    scheduledClass.EnrollmentId, 
                    scheduledClass.ScheduledTime);

                if (accessRecord == null)
                {
                    _logger.LogWarning($"Aluno {scheduledClass.Enrollment.Student.Name} não registrou acesso no horário agendado: {scheduledClass.ScheduledTime}");
                    
                    // Gerar multa
                    var finePayment = new Payment(
                        scheduledClass.Enrollment.Id,
                        FINE_AMOUNT,
                        DateTime.UtcNow.AddDays(7) // vencimento em 7 dias
                    );
                    // O status já será Pending por padrão no construtor
                    await _paymentRepository.AddAsync(finePayment);

                    _logger.LogInformation($"Multa gerada para o aluno {scheduledClass.Enrollment.Student.Name} no valor de {FINE_AMOUNT:C} (pagamento pendente salvo no banco).");
                }
            }
        }
    }
} 