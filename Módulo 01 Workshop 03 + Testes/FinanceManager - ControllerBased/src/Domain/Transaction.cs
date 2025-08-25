using FinanceManager.Infra;

namespace FinanceManager.Domain;

public class Transaction : Entity
{
    private Transaction() { }

    private Transaction(string description, decimal value, decimal usdValue, TransactionCategory category, BankAccount account)
    {
        Description = description;
        Value = value;
        UsdValue = usdValue;
        Type = category.Type;
        CategoryId = category.Id;
        AccountId = account.Id;
    }

    public static Result<Transaction> Create(
        string description, 
        decimal value, 
        decimal usdValue, 
        TransactionCategory category, 
        BankAccount account)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(account);

        if (string.IsNullOrWhiteSpace(description))
        {
            return ValidationErrors.Transaction.DescriptionIsRequired;
        }

        if (value <= 0)
        {
            return ValidationErrors.Transaction.ValueShouldBeGreaterThanZero;
        }

        if (usdValue <= 0)
        {
            return ValidationErrors.Transaction.UsdValueShouldBeGreaterThanZero;
        }

        return new Transaction(description, value, usdValue, category, account);
    }

    public string Description { get; private set; }
    public decimal Value { get; init; }
    public decimal UsdValue { get; init; }
    public TransactionType Type { get; init; }
    public string AccountId { get; init; }
    public string CategoryId { get; init; }
}