using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.ValueObjects;
using FinanceManager.TransactionCategories;

namespace FinanceManager.BankAccounts;

public class BudgetAlert : Entity
{
    private BudgetAlert()
    {

    }

    private BudgetAlert(BankAccount account, TransactionCategory category, Money value)
    {
        AccountId = account.Id;
        CategoryId = category.Id;
        MaxAllowedValue = value;
    }

    public static Result<BudgetAlert> Create(
        BankAccount account,
        TransactionCategory category,
        decimal maxAllowed)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(category);

        var maxAllowedMoneyResult = Money.Create(maxAllowed);
        if (!maxAllowedMoneyResult.TryGetValue(out Money valueAsMoney))
        {
            return Result<BudgetAlert>.Fail(maxAllowedMoneyResult.Errors);
        }

        if (category.Type != TransactionType.Expense)
        {
            return Result<BudgetAlert>.Fail(ValidationErrors.BudgetAlert.CategoryShouldBeExpense);
        }

        return new BudgetAlert(account, category, valueAsMoney);
    }

    public Money MaxAllowedValue { get; private set; }
    public string CategoryId { get; init; }
    public string AccountId { get; init; }

    public bool HasExceedMaxAllowed(BankAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(account.Transactions);

        var transactionsSum = account.Transactions.Sum(t => t.Value);

        return transactionsSum > MaxAllowedValue;
    }
}
