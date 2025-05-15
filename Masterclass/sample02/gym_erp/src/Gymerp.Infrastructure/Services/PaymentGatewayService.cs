using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Gymerp.Application.Interfaces;
using Gymerp.Domain.Entities;

namespace Gymerp.Infrastructure.Services
{
    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly ILogger<PaymentGatewayService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public PaymentGatewayService(
            ILogger<PaymentGatewayService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiKey = _configuration["PagarMe:ApiKey"];
            _apiUrl = _configuration["PagarMe:ApiUrl"];
        }

        public async Task<bool> ProcessCreditCardPaymentAsync(Payment payment, string cardNumber, string expirationDate, string cvv)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PagarMe");
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {_apiKey}");

                var request = new
                {
                    items = new[]
                    {
                        new
                        {
                            amount = (int)(payment.Amount * 100), // Converte para centavos
                            description = $"Pagamento - Matrícula {payment.EnrollmentId}",
                            quantity = 1
                        }
                    },
                    payments = new[]
                    {
                        new
                        {
                            payment_method = "credit_card",
                            credit_card = new
                            {
                                card = new
                                {
                                    number = cardNumber,
                                    holder_name = payment.Enrollment.Student.Name,
                                    exp_month = int.Parse(expirationDate.Split('/')[0]),
                                    exp_year = int.Parse(expirationDate.Split('/')[1]),
                                    cvv = cvv
                                }
                            }
                        }
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation($"Enviando requisição de pagamento para o Pagar.me - Valor: {payment.Amount:C}");

                var response = await client.PostAsync($"{_apiUrl}/orders", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Erro ao processar pagamento: {responseContent}");
                    return false;
                }

                var result = JsonSerializer.Deserialize<dynamic>(responseContent);
                var status = result.GetProperty("status").GetString();

                if (status == "paid")
                {
                    _logger.LogInformation($"Pagamento aprovado com sucesso - ID: {result.GetProperty("id").GetString()}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Pagamento não aprovado - Status: {status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pagamento com Pagar.me");
                return false;
            }
        }
    }
} 