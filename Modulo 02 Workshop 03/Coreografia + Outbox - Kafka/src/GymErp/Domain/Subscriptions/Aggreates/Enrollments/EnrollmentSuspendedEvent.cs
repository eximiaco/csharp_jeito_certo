using GymErp.Common;

namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments;

public record EnrollmentSuspendedEvent(Guid EnrollmentId, DateTime SuspensionStartDate, DateTime SuspensionEndDate) : IDomainEvent;
