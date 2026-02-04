using CSharpFunctionalExtensions;
using GymErp.Common;

namespace GymErp.Domain.Financial.Features.Payments.Domain;

public sealed class Payment : Aggregate
{
    private Payment() { }

    private Payment(Guid id, Guid enrollmentId, decimal amount, string currency, PaymentStatus status, DateTime createdAt, string? gatewayTransactionId)
    {
        Id = id;
        EnrollmentId = enrollmentId;
        Amount = amount;
        Currency = currency;
        Status = status;
        CreatedAt = createdAt;
        GatewayTransactionId = gatewayTransactionId;
    }

    public Guid Id { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? GatewayTransactionId { get; private set; }

    internal void SetGatewayTransactionId(string transactionId)
    {
        GatewayTransactionId = transactionId;
    }

    public static Result<Payment> Process(Guid enrollmentId, decimal amount, string currency)
    {
        if (amount <= 0)
            return Result.Failure<Payment>("O valor do pagamento deve ser maior que zero");

        if (string.IsNullOrWhiteSpace(currency))
            return Result.Failure<Payment>("A moeda não pode ser vazia");

        var payment = new Payment(
            Guid.NewGuid(),
            enrollmentId,
            amount,
            currency,
            PaymentStatus.Processed,
            DateTime.UtcNow,
            gatewayTransactionId: null);

        payment.AddDomainEvent(new PaymentProcessedEvent(payment.Id, payment.EnrollmentId, payment.CreatedAt));

        return Result.Success(payment);
    }
}
