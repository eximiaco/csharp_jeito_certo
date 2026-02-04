using GymErp.Common;

namespace GymErp.Domain.Subscriptions.Features.Enrollments.Domain;

public record EnrollmentCanceledEvent(Guid EnrollmentId, DateTime CanceledAt) : IDomainEvent;
