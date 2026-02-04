namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

public class NewEnrollmentFlowData
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public Guid EnrollmentId { get; set; }
    public bool EnrollmentCreated { get; set; }
    public bool PaymentProcessed { get; set; }
    public bool EvaluationScheduled { get; set; }

    // Propriedades necessárias para o AddEnrollmentStep
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public int AddEnrollmentResult { get; set; } = 0;
}