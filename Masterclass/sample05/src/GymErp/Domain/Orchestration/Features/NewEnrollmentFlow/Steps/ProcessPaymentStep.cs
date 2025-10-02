using Flurl;
using Flurl.Http;
using GymErp.Common;
using GymErp.Common.Settings;
using Microsoft.Extensions.Options;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;

public class ProcessPaymentStep(IOptions<ServicesSettings> options) : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as NewEnrollmentFlowData;
        
        ProcessPaymentRequest request = new(data!.LegacyEnrollmentId);

        var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () => 
            await options.Value.LegacyApiUri
                .AppendPathSegment("api/enrollment/process-payment")
                .PostJsonAsync(request));

        if (response.Outcome == OutcomeType.Failure)
            throw response.FinalException;
        if (!response.Result.ResponseMessage.IsSuccessStatusCode)
            throw new InvalidOperationException("Falha processando pagamento no sistema legado.");

        data.PaymentProcessed = true;
        return ExecutionResult.Next();
    }

    public record ProcessPaymentRequest(Guid EnrollmentId);
}