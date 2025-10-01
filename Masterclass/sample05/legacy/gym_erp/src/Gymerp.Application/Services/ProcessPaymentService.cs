using System;
using System.Threading.Tasks;
using Gymerp.Application.DTOs;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Models;
using Gymerp.Domain.Interfaces;

namespace Gymerp.Application.Services
{
    public class ProcessPaymentService(IEnrollmentRepository enrollmentRepository, IPaymentService paymentService)
        : IProcessPaymentService
    {
        public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentDto dto)
        {
            // 1. Busca a matrícula
            var enrollment = await enrollmentRepository.GetByIdAsync(dto.EnrollmentId);
            if (enrollment == null)
                throw new InvalidOperationException("Matrícula não encontrada");

            // 2. Processa o pagamento
            var paymentResult = await paymentService.ProcessAsync(enrollment);
            
            return paymentResult;
        }
    }
}
