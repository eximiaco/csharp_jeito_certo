using GymErp.Domain.Common;

namespace GymErp.Domain.Enrollments;

public record EnrollmentCreatedEvent(Guid EnrollmentId) : IDomainEvent;
