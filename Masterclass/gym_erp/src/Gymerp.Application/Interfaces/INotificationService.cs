using System.Threading.Tasks;

namespace Gymerp.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(string to, string subject, string message);
    }
} 