using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.BankAccounts.Endpoints;

public record struct CreateBudgetAlertRequest(
    decimal MaxAllowedValue,
    string CategoryId);

public record struct CreateBudgetAlertResponse(
    string Id,
    decimal MaxAllowedValue,
    string CategoryId);

public class CreateBudgetAlertEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("bank-accounts/{accountId}/budget-alerts", CreateBudgetAlertAsync)
            .WithTags("budget-alert")
            .RequireAuthorization()
            .WithName("Create a new budget alert");
    }

    private async static Task<IResult> CreateBudgetAlertAsync(
        [FromRoute] string accountId,
        CreateBudgetAlertRequest request,
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

        var category = await context.GetCategoryAsync(request.CategoryId, cancellationToken: cancellationToken);
        if (category == null)
        {
            return BadRequest(ValidationErrors.Category.CategoryDoesNotExists);
        }

        var budgetAlertResult = BudgetAlert.Create(account, category, request.MaxAllowedValue);
        if (!budgetAlertResult.TryGetValue(out BudgetAlert budgetAlert, out var errors))
        {
            return BadRequest(errors);
        }

        account.AddBudgetAlert(budgetAlert);

        await context.SaveChangesAsync(cancellationToken);

        return Ok(new CreateBudgetAlertResponse(
            budgetAlert.Id,
            budgetAlert.MaxAllowedValue,
            budgetAlert.CategoryId
        ));
    }
}
