using Asp.Versioning;
using FinanceManager.Application;
using MediatR;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FinanceManager.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{v:apiVersion}/user-account")]
public class UserAccountController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUserAsync(
        [FromServices] IMediator mediator,
        [FromBody] CreateUserRequest request)
    {
        var result = await mediator.Send(request);
        return result.ToActionResult(this);
    }

    [ProducesResponseType(typeof(AccessTokenResponse), (int)HttpStatusCode.OK)]
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        [FromServices] IMediator mediator,
        LoginRequest request)
    {
        var result = await mediator.Send(request);
        return result.ToActionResult(this);
    }
}
