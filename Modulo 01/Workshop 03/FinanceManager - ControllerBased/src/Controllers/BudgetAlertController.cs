using FinanceManager.Application;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Controllers;

[ApiController]
[Route("budget-alert")]
public class BudgetAlertController(CreateBudgetAlertaCommandHandler handler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateBudgetAlert(
        [FromBody] CreateBudgetAlertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
