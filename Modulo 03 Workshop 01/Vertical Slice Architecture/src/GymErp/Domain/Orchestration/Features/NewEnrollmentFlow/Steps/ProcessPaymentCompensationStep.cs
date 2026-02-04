using Flurl;
using Flurl.Http;
using GymErp.Common;
using GymErp.Common.Settings;
using Microsoft.Extensions.Options;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;

public class ProcessPaymentCompensationStep(IOptions<ServicesSettings> options) : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as NewEnrollmentFlowData;
        
        if (data?.PaymentProcessed == true)
        {
            var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await options.Value.ProcessPaymentUri
                    .AppendPathSegment($"refund/{data.EnrollmentId}")
                    .PostAsync();
            });

            if (response.Outcome == OutcomeType.Failure)
                throw response.FinalException;
            if (!response.Result.ResponseMessage.IsSuccessStatusCode)
                throw new InvalidOperationException("Falha ao compensar pagamento.");
        }

        return ExecutionResult.Next();
    }
} 