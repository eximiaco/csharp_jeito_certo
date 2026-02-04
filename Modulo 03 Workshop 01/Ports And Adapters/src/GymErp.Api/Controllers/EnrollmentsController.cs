using GymErp.Application.UseCases.AddNewEnrollment;
using GymErp.Application.UseCases.CancelEnrollment;
using GymErp.Application.UseCases.SuspendEnrollment;
using GymErp.Application.Ports.Inbound;
using Microsoft.AspNetCore.Mvc;

namespace GymErp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IAddNewEnrollmentUseCase _addNewEnrollmentUseCase;
    private readonly ICancelEnrollmentUseCase _cancelEnrollmentUseCase;
    private readonly ISuspendEnrollmentUseCase _suspendEnrollmentUseCase;

    public EnrollmentsController(
        IAddNewEnrollmentUseCase addNewEnrollmentUseCase,
        ICancelEnrollmentUseCase cancelEnrollmentUseCase,
        ISuspendEnrollmentUseCase suspendEnrollmentUseCase)
    {
        _addNewEnrollmentUseCase = addNewEnrollmentUseCase;
        _cancelEnrollmentUseCase = cancelEnrollmentUseCase;
        _suspendEnrollmentUseCase = suspendEnrollmentUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddNewEnrollment([FromBody] AddNewEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _addNewEnrollmentUseCase.HandleAsync(request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{enrollmentId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelEnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelEnrollment([FromRoute] Guid enrollmentId, [FromBody] CancelEnrollmentRequest? request, CancellationToken cancellationToken)
    {
        var req = request ?? new CancelEnrollmentRequest { EnrollmentId = enrollmentId };
        if (req.EnrollmentId != enrollmentId)
            req = req with { EnrollmentId = enrollmentId };
        var result = await _cancelEnrollmentUseCase.HandleAsync(req, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{enrollmentId:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SuspendEnrollment([FromRoute] Guid enrollmentId, [FromBody] SuspendEnrollmentCommand command, CancellationToken cancellationToken)
    {
        if (command.EnrollmentId != enrollmentId)
            command = new SuspendEnrollmentCommand(enrollmentId, command.SuspensionStartDate, command.SuspensionEndDate);
        var result = await _suspendEnrollmentUseCase.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}
