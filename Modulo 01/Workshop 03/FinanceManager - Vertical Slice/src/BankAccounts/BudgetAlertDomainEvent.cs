using FinanceManager.Crosscutting;

namespace FinanceManager.BankAccounts;

public class BudgetAlertDomainEvent(BudgetAlert budgetAlert) : IDomainEvent
{
    public BudgetAlert BudgetAlert { get; } = budgetAlert;
}

