using Asp.Versioning;
using FinanceManager.Application;
using FinanceManager.Infra;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/transaction-categories")]
public class TransactionCategoryController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateTransactionCategory(
        [FromServices] IMediator mediator,
        [FromBody] CreateTransactionCategoryRequest transactionRequest)
    {
        var result = await mediator.Send(transactionRequest);
        return result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactionCategories([FromServices] FinanceManagerDbContext context) =>
        Ok(await context.TransactionCategories.ToListAsync());
}
