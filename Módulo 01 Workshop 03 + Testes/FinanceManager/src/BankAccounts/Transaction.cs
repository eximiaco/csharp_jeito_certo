using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.ValueObjects;
using FinanceManager.TransactionCategories;

namespace FinanceManager.BankAccounts;

public class Transaction : Entity
{
    private Transaction() { }

    private Transaction(
        string description,
        Money value,
        Money usdValue,
        TransactionCategory category,
        BankAccount account)
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
        decimal? usdValue,
        TransactionCategory category,
        BankAccount account)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(account);

        if (string.IsNullOrWhiteSpace(description))
        {
            return ValidationErrors.Transaction.DescriptionIsRequired;
        }

        if (!Money.Create(value).TryGetValue(out var parsedValue, out var valueErrors))
        {
            return Result<Transaction>.Fail(valueErrors);
        }

        Money parsedUsdValue = Money.Default with { Currency = "USD" };
        if (usdValue != null && !Money.Create(usdValue.Value, "USD").TryGetValue(out parsedUsdValue, out var usdValueErrors))
        {
            return Result<Transaction>.Fail(usdValueErrors);
        }

        return new Transaction(description, parsedValue, parsedUsdValue, category, account);
    }

    public Result SetUsdValue(decimal value)
    {
        var moneyResult = Money.Create(value, currency: "USD");
        if (!moneyResult.TryGetValue(out Money usdValue))
        {
            return Result.Fail(moneyResult.Errors);
        }

        UsdValue = usdValue;
        UpdatedAt = DateTime.UtcNow;
        return Result.Ok();
    }

    public Money Value { get; init; }
    public TransactionType Type { get; init; }
    public string AccountId { get; init; }
    public string CategoryId { get; init; }
    public string Description { get; private set; }
    public Money UsdValue { get; private set; }
}