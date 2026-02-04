using FastEndpoints;

namespace GymErp.Domain.Subscriptions.Features.SuspendEnrollment;

public class Endpoint : Endpoint<SuspendEnrollmentCommand, bool>
{
    private readonly Handler _handler;

    public Endpoint(Handler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/enrollments/{EnrollmentId}/suspend");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SuspendEnrollmentCommand req, CancellationToken ct)
    {
        var result = await _handler.HandleAsync(req);
        await SendOkAsync(result, ct);
    }
} 