using System.Threading.Tasks;
using Gymerp.Application.DTOs;
using Gymerp.Application.Models;

namespace Gymerp.Application.Interfaces
{
    public interface IProcessPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentDto dto);
    }
}
