namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public record Request
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public StudentDto Student { get; set; } = new();
    public PhysicalAssessmentDto PhysicalAssessment { get; set; } = new();
}

public record StudentDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public record PhysicalAssessmentDto
{
    public Guid PersonalId { get; set; }
    public DateTime AssessmentDate { get; set; }
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal BodyFatPercentage { get; set; }
    public string Notes { get; set; } = string.Empty;
}
