namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

public readonly record struct Request(
    Guid ClientId,
    Guid PlanId,
    string Name,
    string Email,
    string Phone,
    string Document,
    DateTime BirthDate,
    string Gender,
    string Address,
    DateTime StartDate,
    DateTime EndDate,
    Guid PersonalId,
    DateTime AssessmentDate,
    decimal Weight,
    decimal Height,
    decimal BodyFatPercentage,
    string Notes = "");