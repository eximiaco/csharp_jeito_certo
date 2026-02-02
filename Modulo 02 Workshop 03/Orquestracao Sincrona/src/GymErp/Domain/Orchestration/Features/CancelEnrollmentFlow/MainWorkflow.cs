using GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.Steps;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow;

public class MainWorkflow : IWorkflow<CancelEnrollmentFlowData>
{
    public string Id => "cancel-enrollment-workflow";
    public int Version => 1;

    public void Build(IWorkflowBuilder<CancelEnrollmentFlowData> builder)
    {
        builder
            .Saga(saga => saga
                .StartWith<CancelEnrollmentStep>()
                    .CompensateWith<CancelEnrollmentCompensationStep>()
                .Then<ProcessRefundStep>()
                    .CompensateWith<ProcessRefundCompensationStep>())
            .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(30));
    }
}
