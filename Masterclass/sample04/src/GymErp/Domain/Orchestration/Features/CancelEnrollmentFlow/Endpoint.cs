using FastEndpoints;
using WorkflowCore.Interface;

namespace GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow;

public class Endpoint(IWorkflowHost workflowHost) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/api/enrollments/cancel-orchestration");
        AllowAnonymous();


        Tags("Orchestration");
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var workflowData = new CancelEnrollmentFlowData
        {
            EnrollmentId = request.EnrollmentId,
            Reason = request.Reason
        };

        var workflowId = await workflowHost.StartWorkflow("cancel-enrollment-workflow", workflowData);
        await SendAsync(new Response(workflowId), cancellation: ct);
    }
}

public record Response(string WorkflowId);
