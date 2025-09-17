using FinanceManager.Infra;

namespace FinanceManager.Domain;

public class BudgetAlert : Entity
{
    private BudgetAlert()
    {

    }

    private BudgetAlert(BankAccount account, TransactionCategory category, decimal value, decimal threshold)
    {
        AccountId = account.Id;
        CategoryId = category.Id;
        Value = value;
        Threshold = threshold;
    }

    public static Result<BudgetAlert> Create(BankAccount account, TransactionCategory category, decimal value, decimal threshold)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(category);

        if (value <= 0)
        {
            return ValidationErrors.BugetAlert.ValueShouldBeGreaterThanZero();
        }

        if (threshold <= 0)
        {
            return ValidationErrors.BugetAlert.ThresholdShouldBeGreaterThanZero();
        }

        return new BudgetAlert(account, category, value, threshold);
    }

    public decimal Value { get; private set; }
    public decimal Threshold { get; private set; }
    public string CategoryId { get; init; }
    public string AccountId { get; init; }

    public bool HasExceedThreshold(BankAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(account.Transactions);

        var transactionsSum = account.Transactions.Sum(t => t.Value);

        return (transactionsSum / Value) > Threshold;
    }
}
