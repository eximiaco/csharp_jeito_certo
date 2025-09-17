namespace FinanceManager.Domain;

public class BudgetAlertDomainEvent(BudgetAlert budgetAlert) : IDomainEvent
{
    public BudgetAlert BudgetAlert { get; } = budgetAlert;
}

