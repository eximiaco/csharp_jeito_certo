using FinanceManager.Domain;
using FinanceManager.Infra;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Application;

public record CreateBudgetAlertRequest(
    decimal Value,
    decimal Threshold,
    string CategoryId,
    string AccountId);

public record CreateBudgetAlertResponse(
    string Id,
    decimal Value,
    decimal Threshold,
    string CategoryId,
    string AccountId);

public class CreateBudgetAlertaCommandHandler(FinanceManagerDbContext FinanceManagerDbContext)
{
    public async Task<CommandResult<CreateBudgetAlertResponse>> Handle(
        CreateBudgetAlertRequest request,
        CancellationToken cancellationToken)
    {
        var account = await FinanceManagerDbContext
            .BankAccounts
            .FirstOrDefaultAsync(ac => ac.Id == request.AccountId, cancellationToken: cancellationToken);
        
        var category = await FinanceManagerDbContext
            .TransactionCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken: cancellationToken);

        if (account == null)
        {
            return ValidationErrors.Account.AccountDoesNotExists;
        }

        if (category == null)
        {
            return ValidationErrors.Category.CategoryDoesNotExists;
        }

        var budgetAlertResult = BudgetAlert.Create(account, category, request.Value, request.Threshold);
        if(!budgetAlertResult.TryGetValue(out BudgetAlert budgetAlert))
        {
            return CommandResult<CreateBudgetAlertResponse>.InvalidInput(budgetAlertResult.Errors);
        }

        account.AddBudgetAlert(budgetAlert);

        await FinanceManagerDbContext.SaveChangesAsync(cancellationToken);

        return new CreateBudgetAlertResponse(
            budgetAlert.Id,
            budgetAlert.Value,
            budgetAlert.Threshold,
            budgetAlert.CategoryId,
            budgetAlert.AccountId
        );
    }
}
