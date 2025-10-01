using System;
using System.Threading.Tasks;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Models;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;

namespace Gymerp.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly INotificationService _notificationService;
        private const int MAX_PAYMENT_ATTEMPTS = 3;
        private const int PAYMENT_EXPIRATION_DAYS = 7;

        public PaymentService(
            IPaymentRepository paymentRepository, 
            IEnrollmentRepository enrollmentRepository,
            INotificationService notificationService)
        {
            _paymentRepository = paymentRepository;
            _enrollmentRepository = enrollmentRepository;
            _notificationService = notificationService;
        }

        public async Task<PaymentResult> ProcessAsync(Enrollment enrollment)
        {
            // Verifica se já existem tentativas de pagamento
            var existingPayments = await _paymentRepository.GetByEnrollmentIdAsync(enrollment.Id);
            if (existingPayments.Count() >= MAX_PAYMENT_ATTEMPTS)
            {
                enrollment.Cancel();
                await _enrollmentRepository.UpdateAsync(enrollment);
                await _notificationService.SendAsync(
                    enrollment.Student.Email,
                    "Inscrição Cancelada",
                    "Sua inscrição foi cancelada devido ao número máximo de tentativas de pagamento excedido."
                );
                return new PaymentResult { Success = false, Message = "Número máximo de tentativas excedido" };
            }

            // Cria um novo registro de pagamento
            var payment = new Payment(
                enrollment.Id, 
                enrollment.Plan.Price, 
                DateTime.UtcNow.AddDays(PAYMENT_EXPIRATION_DAYS)
            );
            await _paymentRepository.AddAsync(payment);

            try
            {
                // Simula integração com gateway de pagamento
                // Aqui você implementaria a chamada real ao gateway
                var paymentApproved = await SimulatePaymentGatewayAsync(payment);
                
                if (paymentApproved)
                {
                    payment.MarkAsPaid();
                    await _paymentRepository.UpdateAsync(payment);
                    
                    enrollment.Confirm();
                    await _enrollmentRepository.UpdateAsync(enrollment);
                    
                    await _notificationService.SendAsync(
                        enrollment.Student.Email,
                        "Pagamento Aprovado",
                        "Seu pagamento foi aprovado e sua matrícula está confirmada!"
                    );
                    
                    return new PaymentResult { Success = true, Message = "Pagamento aprovado" };
                }
                else
                {
                    payment.MarkAsOverdue();
                    await _paymentRepository.UpdateAsync(payment);
                    
                    await _notificationService.SendAsync(
                        enrollment.Student.Email,
                        "Pagamento Não Aprovado",
                        "Seu pagamento não foi aprovado. Por favor, tente novamente ou entre em contato com o suporte."
                    );
                    
                    return new PaymentResult { Success = false, Message = "Pagamento não aprovado" };
                }
            }
            catch
            {
                payment.MarkAsCancelled();
                await _paymentRepository.UpdateAsync(payment);
                
                await _notificationService.SendAsync(
                    enrollment.Student.Email,
                    "Erro no Processamento do Pagamento",
                    "Ocorreu um erro ao processar seu pagamento. Por favor, tente novamente mais tarde."
                );
                
                return new PaymentResult { Success = false, Message = "Erro no processamento do pagamento" };
            }
        }

        private async Task<bool> SimulatePaymentGatewayAsync(Payment payment)
        {
            // Simula uma chamada ao gateway de pagamento
            // Em produção, substitua por uma integração real
            await Task.Delay(1000); // Simula latência
            return new Random().Next(2) == 1; // 50% de chance de aprovação
        }
    }
} 