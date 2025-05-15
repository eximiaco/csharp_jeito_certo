using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Gymerp.Application.Interfaces;
using Gymerp.Domain.Entities;

namespace Gymerp.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string message)
        {
            // Simula o envio de email
            _logger.LogInformation($"Email enviado para {to}");
            _logger.LogInformation($"Assunto: {subject}");
            _logger.LogInformation($"Mensagem: {message}");
            await Task.CompletedTask;
        }

        public async Task SendPaymentApprovedNotificationAsync(Payment payment)
        {
            var subject = "Pagamento Aprovado";
            var message = $"Seu pagamento no valor de {payment.Amount:C} foi aprovado com sucesso!";
            
            await SendAsync(
                payment.Enrollment.Student.Email,
                subject,
                message
            );
        }

        public async Task SendPaymentRejectedNotificationAsync(Payment payment)
        {
            var subject = "Pagamento Não Aprovado";
            var message = $"Seu pagamento no valor de {payment.Amount:C} não foi aprovado. Por favor, tente novamente ou entre em contato com o suporte.";
            
            await SendAsync(
                payment.Enrollment.Student.Email,
                subject,
                message
            );
        }
    }
} 