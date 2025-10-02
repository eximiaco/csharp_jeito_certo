using FastEndpoints;
using CSharpFunctionalExtensions;

namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class Endpoint : Endpoint<Request, Response>
{
    private readonly Handler _handler;

    public Endpoint(Handler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/enrollments-orchestrator");
        AllowAnonymous();
        
        Tags("Orchestration");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var result = await _handler.HandleAsync(req);
        
        if (result.IsFailure)
        {
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
