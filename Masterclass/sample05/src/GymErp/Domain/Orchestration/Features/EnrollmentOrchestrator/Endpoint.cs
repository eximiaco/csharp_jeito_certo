using FastEndpoints;
using CSharpFunctionalExtensions;

namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class Endpoint(Handler handler) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/api/orchestrator/enroll");
        AllowAnonymous();
        
        Tags("Orchestration");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var result = await handler.HandleAsync(req);
        
        if (result.IsFailure)
        {
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
