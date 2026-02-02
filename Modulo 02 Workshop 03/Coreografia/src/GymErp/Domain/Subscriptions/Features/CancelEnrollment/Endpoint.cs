using FastEndpoints;
using GymErp.Common;

namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment;

public class Endpoint(IServiceBus serviceBus) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/api/enrollments/{EnrollmentId}/cancel");
        AllowAnonymous();
        Tags("Subscriptions");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        await serviceBus.PublishAsync(new CancelEnrollmentCommand(req.EnrollmentId, req.Reason));
        await SendAsync(new Response("Cancelamento aceito para processamento"), statusCode: 202, cancellation: ct);
    }
}
