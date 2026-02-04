namespace GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging;

public record ProcessChargingCommand(Guid EnrollmentId, decimal Amount, string Currency);
