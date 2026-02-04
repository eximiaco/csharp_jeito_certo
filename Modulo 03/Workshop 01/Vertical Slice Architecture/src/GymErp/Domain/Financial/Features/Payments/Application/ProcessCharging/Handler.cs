using CSharpFunctionalExtensions;
using GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging.Adapters;
using GymErp.Domain.Financial.Features.Payments.Domain;
using GymErp.Domain.Financial.Features.Payments.Services;
using GymErp.Domain.Financial.Infrastructure.Persistencia;

namespace GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging;

public class Handler(
    PaymentRepository paymentRepository,
    IFinancialUnitOfWork unitOfWork,
    PaymentChargeSemanticService chargeSemanticService)
{
    public async Task<Result<Response>> HandleAsync(ProcessChargingCommand command, CancellationToken cancellationToken)
    {
        var paymentResult = Payment.Process(command.EnrollmentId, command.Amount, command.Currency);
        if (paymentResult.IsFailure)
            return Result.Failure<Response>(paymentResult.Error);

        var payment = paymentResult.Value;

        var chargeResult = await chargeSemanticService.ChargeAsync(
            payment.Amount,
            payment.Currency,
            payment.EnrollmentId.ToString(),
            description: $"Cobrança matrícula {payment.EnrollmentId}",
            cancellationToken);

        if (!chargeResult.Success)
            return Result.Failure<Response>(chargeResult.ErrorMessage ?? "Cobrança recusada pelo provedor");

        payment.SetGatewayTransactionId(chargeResult.TransactionId!);

        await paymentRepository.AddAsync(payment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        var response = new Response
        {
            PaymentId = payment.Id,
            EnrollmentId = payment.EnrollmentId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            CreatedAt = payment.CreatedAt
        };

        return Result.Success(response);
    }
}
