using FastEndpoints;
using WorkflowCore.Interface;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

public class NewEnrollmentEndpoint : Endpoint<NewEnrollmentFlowData, NewEnrollmentResponse>
{
    private readonly IWorkflowHost _workflowHost;

    public NewEnrollmentEndpoint(IWorkflowHost workflowHost)
    {
        _workflowHost = workflowHost;
    }

    public override void Configure()
    {
        Post("/api/enrollments");
        AllowAnonymous();
    }

    public override async Task HandleAsync(NewEnrollmentFlowData request, CancellationToken ct)
    {
        var workflowId = await _workflowHost.StartWorkflow("new-enrollment-workflow", request);
        await SendAsync(new NewEnrollmentResponse(workflowId), cancellation: ct);
    }
}

public record NewEnrollmentResponse(string WorkflowId);