using System.Threading.Tasks;
using Gymerp.Domain.Entities;

namespace Gymerp.Application.Interfaces
{
    public interface IPaymentGatewayService
    {
        /// <summary>
        /// Processa um pagamento via cartão de crédito
        /// </summary>
        /// <param name="payment">Pagamento a ser processado</param>
        /// <param name="cardNumber">Número do cartão de crédito</param>
        /// <param name="expirationDate">Data de expiração do cartão (MM/YY)</param>
        /// <param name="cvv">Código de segurança do cartão</param>
        /// <returns>True se o pagamento foi aprovado, False caso contrário</returns>
        Task<bool> ProcessCreditCardPaymentAsync(Payment payment, string cardNumber, string expirationDate, string cvv);
    }
} 