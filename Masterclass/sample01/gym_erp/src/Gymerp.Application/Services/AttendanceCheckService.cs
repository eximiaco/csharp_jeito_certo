using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Gymerp.Application.Interfaces;
using Gymerp.Domain.Interfaces;

namespace Gymerp.Application.Services
{
    public class AttendanceCheckService : IAttendanceCheckService
    {
        private readonly IScheduledClassRepository _scheduledClassRepository;
        private readonly IAccessRecordRepository _accessRecordRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<AttendanceCheckService> _logger;
        private const decimal FINE_AMOUNT = 10.00m;

        public AttendanceCheckService(
            IScheduledClassRepository scheduledClassRepository,
            IAccessRecordRepository accessRecordRepository,
            IPaymentRepository paymentRepository,
            ILogger<AttendanceCheckService> logger)
        {
            _scheduledClassRepository = scheduledClassRepository;
            _accessRecordRepository = accessRecordRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task CheckAttendanceAsync()
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
                    var finePayment = new Gymerp.Domain.Entities.Payment(
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