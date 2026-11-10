using GymErp.Adapters.Http.Presentation;
using GymErp.Core.Ports.Driving;
using Microsoft.AspNetCore.Mvc;

namespace GymErp.Adapters.Http.Controllers;

// Adaptador de condução HTTP: traduz HTTP para a porta IForManagingEnrollments e o resultado de volta.
[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController(IForManagingEnrollments enrollments) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request, CancellationToken cancellationToken) =>
        (await enrollments.EnrollAsync(request, cancellationToken)).ToHttp();

    [HttpPost("{enrollmentId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel([FromRoute] Guid enrollmentId, [FromBody] CancelBody? body, CancellationToken cancellationToken) =>
        (await enrollments.CancelAsync(new CancelRequest(enrollmentId, body?.Reason ?? string.Empty), cancellationToken)).ToHttp();

    [HttpPost("{enrollmentId:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Suspend([FromRoute] Guid enrollmentId, [FromBody] SuspendBody body, CancellationToken cancellationToken) =>
        (await enrollments.SuspendAsync(new SuspendRequest(enrollmentId, body.SuspensionStartDate, body.SuspensionEndDate), cancellationToken)).ToHttp();

    // Corpos HTTP: o id vem da rota.
    public sealed record CancelBody(string Reason);
    public sealed record SuspendBody(DateTime SuspensionStartDate, DateTime SuspensionEndDate);
}
