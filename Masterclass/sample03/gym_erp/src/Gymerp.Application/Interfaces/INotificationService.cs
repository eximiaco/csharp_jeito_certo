using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendPaymentConfirmationAsync(Payment payment);
        Task SendPaymentRejectionAsync(Payment payment);
    }
} 