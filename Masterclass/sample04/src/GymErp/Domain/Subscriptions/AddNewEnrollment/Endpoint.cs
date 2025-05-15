using FastEndpoints;
using Gymerp.Domain.Subscriptions.Infrastructure;

namespace Gymerp.Domain.Subscriptions.AddNewEnrollment;

public class Endpoint : Endpoint<Request, Response>
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

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        var enrollmentId = await _handler.HandleAsync(request, ct);
        await SendAsync(new Response { EnrollmentId = enrollmentId }, cancellation: ct);
    }
}