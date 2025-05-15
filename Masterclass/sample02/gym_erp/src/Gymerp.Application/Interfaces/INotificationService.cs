using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(string to, string subject, string message);
        Task SendPaymentApprovedNotificationAsync(Payment payment);
        Task SendPaymentRejectedNotificationAsync(Payment payment);
    }
} 