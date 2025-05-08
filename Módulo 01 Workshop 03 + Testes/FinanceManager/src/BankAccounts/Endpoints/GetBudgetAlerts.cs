using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.BankAccounts.Endpoints;

public record struct GetBudgetAlertResponse(
    string Id,
    decimal MaxAllowedValue,
    string CategoryId);

public class GetBudgetAlertsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("bank-accounts/{accountId}/budget-alerts", GetBudgetAlertsAsync)
            .WithTags("budget-alert")
            .RequireAuthorization()
            .WithName("Get all budget alerts");
    }

    private async static Task<IResult> GetBudgetAlertsAsync(
        [FromRoute] string accountId,
        FinanceManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var account = await context
            .BankAccounts
            .Include(b => b.BudgetAlerts)
            .FirstOrDefaultAsync(b => b.Id == accountId, cancellationToken);

        if (account == null)
        {
            return BadRequest(ValidationErrors.Account.AccountDoesNotExists);
        }

        return Ok(account.BudgetAlerts.Select(a => new GetBudgetAlertResponse(
            a.Id,
            a.MaxAllowedValue,
            a.CategoryId
        )));
    }
}
