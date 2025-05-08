using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.ValueObjects;
using FinanceManager.TransactionCategories;
using FinanceManager.UserAccounts;

namespace FinanceManager.BankAccounts;

public class BankAccount : Entity
{
    private readonly List<Transaction> _transactions = [];
    private readonly List<BudgetAlert> _budgetAlerts = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private BankAccount() { }

    private BankAccount(string name, User user)
    {
        UpdatedAt = DateTime.UtcNow;
        Balance = Money.Default;
        Name = name;
        User = user;

        _transactions = [];
        _budgetAlerts = [];
        _domainEvents = [];
    }

    public static Result<BankAccount> Create(string name, User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationErrors.BankAccount.NameIsRequired;
        }

        return new BankAccount(name, user);
    }

    public string Name { get; private set; }
    public Money Balance { get; private set; }
    public IEnumerable<Transaction> Transactions => _transactions;
    public IEnumerable<BudgetAlert> BudgetAlerts => _budgetAlerts;
    public IEnumerable<IDomainEvent> Events => _domainEvents;

    public User User { get; private set; }

    public void AddTransaction(Transaction transaction)
    {
        if (transaction.Type == TransactionType.Income)
        {
            Balance += transaction.Value;
        }
        else
        {
            Balance -= transaction.Value;
        }

        _transactions.Add(transaction);

        var alerts = _budgetAlerts.Where(ba => ba.HasExceedMaxAllowed(this));

        foreach (var alert in alerts)
        {
            _domainEvents.Add(new BudgetAlertDomainEvent(alert));
        }
    }

    public void AddBudgetAlert(BudgetAlert budgetAlert)
    {
        _budgetAlerts.Add(budgetAlert);
    }
}