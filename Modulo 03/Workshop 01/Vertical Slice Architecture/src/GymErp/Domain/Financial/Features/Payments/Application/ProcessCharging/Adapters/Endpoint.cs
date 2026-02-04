using FastEndpoints;
using GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging;

namespace GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging.Adapters;

public record Request
{
    public Guid EnrollmentId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}

public record Response
{
    public Guid PaymentId { get; set; }
    public Guid EnrollmentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class Endpoint(Handler handler) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/api/payments/process-charging");
        AllowAnonymous();
        Tags("Financial");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var command = new ProcessChargingCommand(
            req.EnrollmentId,
            req.Amount ?? 99.90m,
            req.Currency ?? "BRL");

        var result = await handler.HandleAsync(command, ct);
        if (result.IsFailure)
        {
            AddError(result.Error);
            await SendErrorsAsync(cancellation: ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}
