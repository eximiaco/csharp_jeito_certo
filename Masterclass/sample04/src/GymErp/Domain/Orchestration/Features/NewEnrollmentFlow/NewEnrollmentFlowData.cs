namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

public class NewEnrollmentFlowData
{
    public Guid ClientId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}