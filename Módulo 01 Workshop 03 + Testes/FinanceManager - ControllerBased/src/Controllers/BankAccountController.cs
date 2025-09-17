using Asp.Versioning;
using FinanceManager.Application;
using FinanceManager.Domain;
using FinanceManager.Infra;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using System.Linq.Expressions;

namespace FinanceManager.Controllers;

[ApiController]
[ApiVersion(1.0)]
[ApiVersion(1.1)]
[Route("api/v{v:apiVersion}/bank-accounts")]
public class BankAccountController(
    FinanceManagerDbContext context,
    ILogger<BankAccountController> logger) : ControllerBase
{
    [Authorize(Policy = "Admin")]
    [MapToApiVersion(1.0)]
    [HttpGet]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] int skip,
        [FromQuery] int take = 10)
    {
        logger.LogInformation("Getting all bank accounts");

        var accounts = await context
            .BankAccounts
            .Include(ac => ac.Transactions)
            .Include(ac => ac.BudgetAlerts)
            .AsNoTracking()
            .Skip(skip)
            .Take(take)
            .Select(bc => new
            {
                bc.Transactions,
                bc.Balance
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [Authorize]
    [MapToApiVersion(1.0)]
    [HttpPost]
    public async Task<IStatusCodeActionResult> CreateAccount(
        [FromServices] IMediator mediator,
        [FromBody] CreateBankAccountRequest request)
    {
        var result = await mediator.Send(request);
        return result.ToActionResult(this);
    }

    [Authorize]
    [MapToApiVersion(1.0)]
    [HttpPost("{accountId}/transactions")]
    public async Task<IActionResult> CreateTransaction(
        [FromRoute] string accountId,
        [FromServices] IMediator mediator,
        [FromBody] CreateTransactionRequest request)
    {
        var command = new CreateTransactionCommand(request, accountId);
        var result = await mediator.Send(command);

        return result.ToActionResult(this);
    }
}
