using FastEndpoints;

namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment;

public class Endpoint(Handler handler) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/api/enrollments/{EnrollmentId}/cancel");
        AllowAnonymous();


        Tags("Subscriptions");
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
