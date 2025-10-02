using Flurl;
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
        
        var request = new ScheduleEvaluationRequest(
            data!.ClientId,
            data.PersonalId,
            data.AssessmentDate,
            data.Weight,
            data.Height,
            data.BodyFatPercentage,
            data.Notes
        );

        var response = await HttpRetryPolicy.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            return await options.Value.LegacyApiUri
                .AppendPathSegment("api/enrollment/schedule-assessment")
                .PostJsonAsync(request);
        });

        if (response.Outcome == OutcomeType.Failure)
            throw response.FinalException;
        if (!response.Result.ResponseMessage.IsSuccessStatusCode)
            throw new InvalidOperationException("Falha ao agendar avaliação no sistema legado.");

        data.EvaluationScheduled = true;
        return ExecutionResult.Next();
    }

    public record ScheduleEvaluationRequest(
        Guid StudentId,
        Guid PersonalId,
        DateTime AssessmentDate,
        decimal Weight,
        decimal Height,
        decimal BodyFatPercentage,
        string Notes
    );
}