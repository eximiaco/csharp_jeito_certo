using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;
using WorkflowCore.Interface;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

public class MainWorkflow : IWorkflow<NewEnrollmentFlowData>
{
    public string Id => nameof(MainWorkflow);
    public int Version => 1;

    public void Build(IWorkflowBuilder<NewEnrollmentFlowData> builder)
    {
        builder
            .StartWith<AddEnrollmentStep>()
            .Then<ProcessPaymentStep>()
            .Then<ScheduleEvaluationStep>();
    }
}