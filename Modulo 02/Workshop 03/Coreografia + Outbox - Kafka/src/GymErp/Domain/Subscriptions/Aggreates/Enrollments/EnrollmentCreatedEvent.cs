using GymErp.Common;

namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments;

public record EnrollmentCreatedEvent(Guid EnrollmentId) : IDomainEvent;