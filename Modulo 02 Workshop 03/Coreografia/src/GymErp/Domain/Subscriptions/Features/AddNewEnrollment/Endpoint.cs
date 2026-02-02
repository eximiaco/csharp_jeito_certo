using FastEndpoints;

namespace GymErp.Domain.Subscriptions.Features.AddNewEnrollment;

public class Endpoint(Handler handler) : Endpoint<Request, Guid>
{
    public override void Configure()
    {
        Post("/api/enrollments");
        AllowAnonymous();
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