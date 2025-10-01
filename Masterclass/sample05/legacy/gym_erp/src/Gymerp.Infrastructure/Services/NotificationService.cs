using System;
using System.Threading.Tasks;
using Gymerp.Application.Interfaces;
using Microsoft.Extensions.Logging;

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
            try
            {
                // TODO: Implementar integração real com serviço de email
                // Por enquanto, apenas logamos a notificação
                _logger.LogInformation(
                    "Notificação enviada para {To}\nAssunto: {Subject}\nMensagem: {Message}",
                    to, subject, message
                );

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação para {To}", to);
                throw;
            }
        }
    }
} 