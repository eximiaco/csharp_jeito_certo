namespace GymErp.Application.UseCases.SuspendEnrollment;

// A regra "suspensão mínima de 30 dias" mora só na entidade (Enrollment.Suspend).
public record SuspendEnrollmentCommand(
    Guid EnrollmentId,
    DateTime SuspensionStartDate,
    DateTime SuspensionEndDate);
