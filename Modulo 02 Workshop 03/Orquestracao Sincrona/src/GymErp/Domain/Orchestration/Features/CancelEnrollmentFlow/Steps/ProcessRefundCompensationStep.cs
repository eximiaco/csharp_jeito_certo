using Flurl;
using Flurl.Http;
using GymErp.Common;
using GymErp.Common.Settings;
using Microsoft.Extensions.Options;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.Steps;

public class ProcessRefundCompensationStep(IOptions<ServicesSettings> options) : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as CancelEnrollmentFlowData;
        
        if (!data!.RefundProcessed || !data.RefundId.HasValue)
            return ExecutionResult.Next();

        // Reverter o estorno processado
        var request = new RevertRefundRequest(data.RefundId.Value, "Compensação de estorno");

        var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            return await options.Value.ProcessPaymentUri
                .AppendPathSegment($"refunds/{data.RefundId}/revert")
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

    public record RevertRefundRequest(Guid RefundId, string Reason);
}
