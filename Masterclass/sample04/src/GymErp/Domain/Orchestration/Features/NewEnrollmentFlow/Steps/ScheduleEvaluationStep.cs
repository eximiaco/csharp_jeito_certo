using Flurl.Http;
using GymErp.Common;
using GymErp.Common.Settings;
using Microsoft.Extensions.Options;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;

public class ScheduleEvaluationStep(IOptions<ServicesSettings> options) : StepBodyAsync
{
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as NewEnrollmentFlowData;
        ScheduleEvaluationRequest request = new(data!.ClientId);

        var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            return await options.Value.ScheduleEvaluationUri
                .PostJsonAsync(request);
        });

        if (response.Outcome == OutcomeType.Failure)
            throw response.FinalException;
        if (!response.Result.ResponseMessage.IsSuccessStatusCode)
            throw new InvalidOperationException("Falha agendando avaliação.");

        return ExecutionResult.Next();
    }

    public record ScheduleEvaluationRequest(Guid ClientId);
}