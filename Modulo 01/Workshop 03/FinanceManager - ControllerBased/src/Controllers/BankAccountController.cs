using Asp.Versioning;
using FinanceManager.Application;
using FinanceManager.Domain;
using FinanceManager.Infra;
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
    ILogger<BankAccountController> logger,
    CreateBankAccountCommandHandler createBankAccountHandler,
    CreateTransactionCommandHandler createTransactionHandler) : ControllerBase
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
        [FromBody] CreateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createBankAccountHandler.Handle(request, cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize]
    [MapToApiVersion(1.0)]
    [HttpPost("{accountId}/transactions")]
    public async Task<IActionResult> CreateTransaction(
        [FromRoute] string accountId,
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransactionCommand(request, accountId);
        var result = await createTransactionHandler.Handle(command, cancellationToken);

        return result.ToActionResult(this);
    }
}
