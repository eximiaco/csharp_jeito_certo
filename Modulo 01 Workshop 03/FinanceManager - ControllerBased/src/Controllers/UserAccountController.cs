using Asp.Versioning;
using FinanceManager.Application;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FinanceManager.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{v:apiVersion}/user-account")]
public class UserAccountController(
    CreateUserCommandHandler createUserHandler,
    LoginCommandHandler loginHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createUserHandler.Handle(request, cancellationToken);
        return result.ToActionResult(this);
    }

    [ProducesResponseType(typeof(AccessTokenResponse), (int)HttpStatusCode.OK)]
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginHandler.Handle(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
