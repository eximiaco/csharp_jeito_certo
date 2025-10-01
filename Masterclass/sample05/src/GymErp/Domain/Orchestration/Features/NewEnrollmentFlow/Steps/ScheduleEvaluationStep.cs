using Flurl.Http;
using GymErp.Common;
using GymErp.Common.Settings;
using Microsoft.Extensions.Options;
using Polly;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;

public class ScheduleEvaluationStep : StepBodyAsync
{
    private readonly IOptions<ServicesSettings> _options;

    public ScheduleEvaluationStep(IOptions<ServicesSettings> options)
    {
        _options = options;
    }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = context.Workflow.Data as NewEnrollmentFlowData;
        
        var request = new ScheduleEvaluationRequest(data!.ClientId);

        var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            return await _options.Value.ScheduleEvaluationUri
                .PostJsonAsync(request);
        });

        if (response.Outcome == OutcomeType.Failure)
            throw response.FinalException;
        if (!response.Result.ResponseMessage.IsSuccessStatusCode)
            throw new InvalidOperationException("Falha ao agendar avaliação.");

        data.EvaluationScheduled = true;
        return ExecutionResult.Next();
    }

    public record ScheduleEvaluationRequest(Guid ClientId);
}