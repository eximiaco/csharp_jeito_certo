using GymErp.Api.Presentation;
using GymErp.Application.UseCases.AddNewEnrollment;
using GymErp.Application.UseCases.CancelEnrollment;
using GymErp.Application.UseCases.SuspendEnrollment;
using Microsoft.AspNetCore.Mvc;

namespace GymErp.Api.Controllers;

// Controller: traduz HTTP para o modelo de requisição do caso de uso e o resultado de volta para HTTP.
// Conhece só tipos da Application; nenhum tipo do Domain aparece aqui.
[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController(
    IAddNewEnrollmentUseCase addNewEnrollment,
    ICancelEnrollmentUseCase cancelEnrollment,
    ISuspendEnrollmentUseCase suspendEnrollment) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddNewEnrollment([FromBody] AddNewEnrollmentRequest request, CancellationToken cancellationToken) =>
        (await addNewEnrollment.HandleAsync(request, cancellationToken)).ToHttp();

    [HttpPost("{enrollmentId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelEnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelEnrollment([FromRoute] Guid enrollmentId, [FromBody] CancelEnrollmentBody? body, CancellationToken cancellationToken) =>
        (await cancelEnrollment.HandleAsync(
            new CancelEnrollmentRequest { EnrollmentId = enrollmentId, Reason = body?.Reason ?? string.Empty },
            cancellationToken)).ToHttp();

    [HttpPost("{enrollmentId:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SuspendEnrollment([FromRoute] Guid enrollmentId, [FromBody] SuspendEnrollmentBody body, CancellationToken cancellationToken) =>
        (await suspendEnrollment.HandleAsync(
            new SuspendEnrollmentCommand(enrollmentId, body.SuspensionStartDate, body.SuspensionEndDate),
            cancellationToken)).ToHttp();

    // Corpos HTTP: o id vem da rota, então não é repetido no corpo.
    public sealed record CancelEnrollmentBody(string Reason);
    public sealed record SuspendEnrollmentBody(DateTime SuspensionStartDate, DateTime SuspensionEndDate);

}
