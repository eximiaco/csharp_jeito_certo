using System;
using System.Threading.Tasks;
using Gymerp.Application.Interfaces;
using Microsoft.Extensions.Logging;
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

        public async Task SendAsync(string to, string subject, string body)
        {
            // Simula envio de e-mail
            _logger.LogInformation($"Email enviado para {to} | Assunto: {subject} | Mensagem: {body}");
            await Task.CompletedTask;
        }

        public async Task SendPaymentConfirmationAsync(Payment payment)
        {
            var subject = "Pagamento aprovado";
            var body = $"Seu pagamento de {payment.Amount:C} foi aprovado com sucesso.";
            var to = payment.Enrollment?.Student?.Email ?? "";
            await SendAsync(to, subject, body);
        }

        public async Task SendPaymentRejectionAsync(Payment payment)
        {
            var subject = "Pagamento não aprovado";
            var body = $"Seu pagamento de {payment.Amount:C} não foi aprovado. Por favor, tente novamente.";
            var to = payment.Enrollment?.Student?.Email ?? "";
            await SendAsync(to, subject, body);
        }
    }
} 