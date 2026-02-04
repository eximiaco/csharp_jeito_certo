using GymErp.Domain.Common;

namespace GymErp.Domain.Enrollments;

public record EnrollmentCanceledEvent(Guid EnrollmentId, DateTime CanceledAt) : IDomainEvent;
