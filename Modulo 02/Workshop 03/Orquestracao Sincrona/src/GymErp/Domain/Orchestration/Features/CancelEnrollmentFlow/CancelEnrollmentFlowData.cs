namespace GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow;

public class CancelEnrollmentFlowData
{
    public Guid EnrollmentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool EnrollmentCanceled { get; set; }
    public bool RefundProcessed { get; set; }
    public Guid? RefundId { get; set; }
}
