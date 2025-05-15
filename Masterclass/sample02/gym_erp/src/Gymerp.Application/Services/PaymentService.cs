using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Gymerp.Application.Interfaces;
using Gymerp.Application.Models;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;

namespace Gymerp.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PaymentService> _logger;
        private const int MAX_PAYMENT_ATTEMPTS = 3;
        private const int PAYMENT_EXPIRATION_DAYS = 7;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IPaymentGatewayService paymentGatewayService,
            IEnrollmentRepository enrollmentRepository,
            INotificationService notificationService,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _paymentGatewayService = paymentGatewayService;
            _enrollmentRepository = enrollmentRepository;
            _notificationService = notificationService;
            _logger = logger;
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
                _logger.LogInformation($"Iniciando processamento do pagamento {payment.Id}");

                // Processa o pagamento via gateway
                var isApproved = await ProcessPaymentAsync(payment, "", "", "");
                
                if (isApproved)
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
                    
                    _logger.LogInformation($"Pagamento {payment.Id} aprovado com sucesso");
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
                    
                    _logger.LogWarning($"Pagamento {payment.Id} rejeitado");
                    return new PaymentResult { Success = false, Message = "Pagamento não aprovado" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar pagamento {payment.Id}");
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

        public async Task<bool> ProcessPaymentAsync(Payment payment, string cardNumber, string expirationDate, string cvv)
        {
            try
            {
                _logger.LogInformation($"Iniciando processamento do pagamento {payment.Id}");

                // Processa o pagamento via gateway
                var isApproved = await _paymentGatewayService.ProcessCreditCardPaymentAsync(
                    payment,
                    cardNumber,
                    expirationDate,
                    cvv);

                if (isApproved)
                {
                    payment.MarkAsApproved();
                    await _paymentRepository.UpdateAsync(payment);

                    // Notifica o aluno sobre o pagamento aprovado
                    await _notificationService.SendPaymentApprovedNotificationAsync(payment);

                    _logger.LogInformation($"Pagamento {payment.Id} aprovado com sucesso");
                    return true;
                }
                else
                {
                    payment.MarkAsRejected();
                    await _paymentRepository.UpdateAsync(payment);

                    // Notifica o aluno sobre o pagamento rejeitado
                    await _notificationService.SendPaymentRejectedNotificationAsync(payment);

                    _logger.LogWarning($"Pagamento {payment.Id} rejeitado");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar pagamento {payment.Id}");
                throw;
            }
        }
    }
} 