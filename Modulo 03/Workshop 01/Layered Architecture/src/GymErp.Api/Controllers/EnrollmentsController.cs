using GymErp.Application.Enrollments.AddNewEnrollment;
using GymErp.Application.Enrollments.CancelEnrollment;
using GymErp.Application.Enrollments.SuspendEnrollment;
using Microsoft.AspNetCore.Mvc;

namespace GymErp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IAddNewEnrollmentService _addNewEnrollmentService;
    private readonly ICancelEnrollmentService _cancelEnrollmentService;
    private readonly ISuspendEnrollmentService _suspendEnrollmentService;

    public EnrollmentsController(
        IAddNewEnrollmentService addNewEnrollmentService,
        ICancelEnrollmentService cancelEnrollmentService,
        ISuspendEnrollmentService suspendEnrollmentService)
    {
        _addNewEnrollmentService = addNewEnrollmentService;
        _cancelEnrollmentService = cancelEnrollmentService;
        _suspendEnrollmentService = suspendEnrollmentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddNewEnrollment([FromBody] AddNewEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _addNewEnrollmentService.HandleAsync(request, cancellationToken);
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
        var result = await _cancelEnrollmentService.HandleAsync(req, cancellationToken);
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
        var result = await _suspendEnrollmentService.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}
