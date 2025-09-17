using FastEndpoints;

namespace GymErp.Domain.Subscriptions.Features.AddNewEnrollment;

public class Endpoint : Endpoint<Request, Guid>
{
    private readonly Handler _handler;

    public Endpoint(Handler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/enrollments");
        AllowAnonymous();
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