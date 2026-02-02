using Flurl;
using Flurl.Http;
using GymErp.Common;
using GymErp.Common.Settings;
using Microsoft.Extensions.Options;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.Steps;

public class CancelEnrollmentCompensationStep(IOptions<ServicesSettings> options) : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as CancelEnrollmentFlowData;
        
        if (!data!.EnrollmentCanceled)
            return ExecutionResult.Next();

        // Reativar a inscrição cancelada
        var request = new ReactivateEnrollmentRequest(data.EnrollmentId, "Compensação de cancelamento");

        var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            return await options.Value.SubscriptionsUri
                .AppendPathSegment($"enrollments/{data.EnrollmentId}/reactivate")
                .PostJsonAsync(request);
        });

        if (response.Outcome == OutcomeType.Failure)
        {
            // Log do erro mas não falha a compensação
            // Em um cenário real, seria importante logar para auditoria
            return ExecutionResult.Next();
        }

        return ExecutionResult.Next();
    }

    public record ReactivateEnrollmentRequest(Guid EnrollmentId, string Reason);
}
