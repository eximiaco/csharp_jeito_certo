using CSharpFunctionalExtensions;
using GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging.Adapters;
using GymErp.Domain.Financial.Features.Payments.Domain;
using GymErp.Domain.Financial.Infrastructure;

namespace GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging;

public class Handler(
    PaymentRepository paymentRepository,
    IFinancialUnitOfWork unitOfWork)
{
    public async Task<Result<Response>> HandleAsync(ProcessChargingCommand command, CancellationToken cancellationToken)
    {
        var paymentResult = Payment.Process(command.EnrollmentId, command.Amount, command.Currency);
        if (paymentResult.IsFailure)
            return Result.Failure<Response>(paymentResult.Error);

        var payment = paymentResult.Value;
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
