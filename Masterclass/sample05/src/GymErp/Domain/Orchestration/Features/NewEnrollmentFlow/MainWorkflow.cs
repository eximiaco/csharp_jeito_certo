using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

public class MainWorkflow : IWorkflow<NewEnrollmentFlowData>
{
    public string Id => "new-enrollment-workflow";
    public int Version => 1;

    public void Build(IWorkflowBuilder<NewEnrollmentFlowData> builder)
    {
        builder
            .Saga(saga => saga
                .StartWith<AddEnrollmentStep>()
                    .CompensateWith<AddEnrollmentCompensationStep>()
                .Then<AddLegacyEnrollmentStep>()
                    .CompensateWith<AddLegacyEnrollmentCompensationStep>()
                .Then<ProcessPaymentStep>()
                    .CompensateWith<ProcessPaymentCompensationStep>()
                .Then<ScheduleEvaluationStep>()
                    .CompensateWith<ScheduleEvaluationCompensationStep>())
            .OnError(WorkflowErrorHandling.Retry, TimeSpan.FromSeconds(30));
    }
}