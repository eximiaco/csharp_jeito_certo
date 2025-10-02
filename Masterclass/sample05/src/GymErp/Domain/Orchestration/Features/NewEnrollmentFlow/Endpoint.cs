using FastEndpoints;
using WorkflowCore.Interface;

namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

public class NewEnrollmentEndpoint(IWorkflowHost workflowHost) : Endpoint<Request, NewEnrollmentResponse>
{
    public override void Configure()
    {
        Post("/api/enrollments");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var workflowData = new NewEnrollmentFlowData
        {
            ClientId = request.ClientId,
            PlanId = request.PlanId,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Document = request.Document,
            BirthDate = request.BirthDate,
            Gender = request.Gender,
            Address = request.Address,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PersonalId = request.PersonalId,
            AssessmentDate = request.AssessmentDate,
            Weight = request.Weight,
            Height = request.Height,
            BodyFatPercentage = request.BodyFatPercentage,
            Notes = request.Notes
        };

        var workflowId = await workflowHost.StartWorkflow("new-enrollment-workflow", workflowData);
        await SendAsync(new NewEnrollmentResponse(workflowId), cancellation: ct);
    }
}

public record NewEnrollmentResponse(string WorkflowId);