using GymErp.Common;

namespace GymErp.Domain.Financial.Features.Payments.Domain;

public record PaymentProcessedEvent(Guid PaymentId, Guid EnrollmentId, DateTime ProcessedAt) : IDomainEvent;
