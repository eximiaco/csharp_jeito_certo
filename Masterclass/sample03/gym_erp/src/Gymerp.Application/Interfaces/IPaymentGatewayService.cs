using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.Interfaces
{
    public interface IPaymentGatewayService
    {
        Task<bool> ProcessCreditCardPaymentAsync(Payment payment, string cardNumber, string expirationDate, string cvv);
    }
} 