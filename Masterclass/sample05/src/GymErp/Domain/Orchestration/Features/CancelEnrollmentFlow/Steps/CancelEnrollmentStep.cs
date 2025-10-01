using Flurl;
using Flurl.Http;
using GymErp.Common;
using GymErp.Common.Settings;
using Microsoft.Extensions.Options;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.Steps;

public class CancelEnrollmentStep(IOptions<ServicesSettings> options) : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as CancelEnrollmentFlowData;
        
        var request = new CancelEnrollmentRequest(data!.EnrollmentId, data.Reason);

        var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            return await options.Value.SubscriptionsUri
                .AppendPathSegment($"enrollments/{data.EnrollmentId}/cancel")
                .PostJsonAsync(request);
        });

        if (response.Outcome == OutcomeType.Failure)
            throw response.FinalException;
        if (!response.Result.ResponseMessage.IsSuccessStatusCode)
            throw new InvalidOperationException("Falha ao cancelar inscrição.");

        var cancelResponse = await response.Result.GetJsonAsync<CancelEnrollmentResponse>();
        data.EnrollmentCanceled = true;
        return ExecutionResult.Next();
    }

    public record CancelEnrollmentRequest(Guid EnrollmentId, string Reason);
    public record CancelEnrollmentResponse(Guid EnrollmentId, DateTime CanceledAt);
}
