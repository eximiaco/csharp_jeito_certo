using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Gymerp.Domain.Interfaces;
using Gymerp.Domain.Entities;
using Gymerp.Application.Interfaces;

namespace Gymerp.Application.Services
{
    public class AttendanceService : IAttendanceCheckService
    {
        private readonly ILogger<AttendanceService> _logger;
        private readonly IAccessRecordRepository _accessRecordRepository;
        private readonly IScheduledClassRepository _scheduledClassRepository;
        private readonly IPaymentRepository _paymentRepository;
        private const decimal FINE_AMOUNT = 10.00m;

        public AttendanceService(
            ILogger<AttendanceService> logger,
            IAccessRecordRepository accessRecordRepository,
            IScheduledClassRepository scheduledClassRepository,
            IPaymentRepository paymentRepository)
        {
            _logger = logger;
            _accessRecordRepository = accessRecordRepository;
            _scheduledClassRepository = scheduledClassRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task CheckTodayAttendancesAsync()
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
                    
                    await GenerateFineForAbsenceAsync(scheduledClass);
                }
            }
        }

        private async Task GenerateFineForAbsenceAsync(ScheduledClass scheduledClass)
        {
            var finePayment = new Payment(
                scheduledClass.Enrollment.Id,
                FINE_AMOUNT,
                DateTime.UtcNow.AddDays(7) // vencimento em 7 dias
            );

            await _paymentRepository.AddAsync(finePayment);

            _logger.LogInformation(
                $"Multa gerada para o aluno {scheduledClass.Enrollment.Student.Name} " +
                $"no valor de {FINE_AMOUNT:C} (pagamento pendente salvo no banco).");
        }
    }
} 