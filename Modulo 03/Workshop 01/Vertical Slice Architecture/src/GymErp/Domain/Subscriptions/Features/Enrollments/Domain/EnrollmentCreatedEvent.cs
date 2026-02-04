using GymErp.Common;

namespace GymErp.Domain.Subscriptions.Features.Enrollments.Domain;

public record EnrollmentCreatedEvent(Guid EnrollmentId) : IDomainEvent;
