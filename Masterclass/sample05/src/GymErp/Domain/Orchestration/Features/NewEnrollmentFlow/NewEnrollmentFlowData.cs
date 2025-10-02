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

    // Novos campos necessários para integração com sistema legado
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid PersonalId { get; set; }
    public DateTime AssessmentDate { get; set; }
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal BodyFatPercentage { get; set; }
    public string Notes { get; set; } = string.Empty;
    
    // Campos para controle do fluxo com sistema legado
    public Guid LegacyEnrollmentId { get; set; }
    public bool LegacyEnrollmentCreated { get; set; }

    public int AddEnrollmentResult { get; set; } = 0;
}