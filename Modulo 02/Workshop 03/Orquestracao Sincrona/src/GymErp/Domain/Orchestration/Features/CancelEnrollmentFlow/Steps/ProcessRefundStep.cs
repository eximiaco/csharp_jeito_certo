using Flurl;
using Flurl.Http;
using GymErp.Common;
using GymErp.Common.Settings;
using Microsoft.Extensions.Options;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.Steps;

public class ProcessRefundStep(IOptions<ServicesSettings> options) : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as CancelEnrollmentFlowData;
        
        var request = new ProcessRefundRequest(data!.EnrollmentId, "Cancelamento de inscrição");

        var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            return await options.Value.ProcessPaymentUri
                .AppendPathSegment("refunds")
                .PostJsonAsync(request);
        });

        if (response.Outcome == OutcomeType.Failure)
            throw response.FinalException;
        if (!response.Result.ResponseMessage.IsSuccessStatusCode)
            throw new InvalidOperationException("Falha ao processar estorno.");

        var refundResponse = await response.Result.GetJsonAsync<ProcessRefundResponse>();
        data.RefundId = refundResponse.RefundId;
        data.RefundProcessed = true;
        return ExecutionResult.Next();
    }

    public record ProcessRefundRequest(Guid EnrollmentId, string Reason);
    public record ProcessRefundResponse(Guid RefundId, decimal Amount, DateTime ProcessedAt);
}
