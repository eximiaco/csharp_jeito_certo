using GymErp.Common;

namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments;

public record EnrollmentCanceledEvent(Guid EnrollmentId, DateTime CanceledAt) : IDomainEvent;
