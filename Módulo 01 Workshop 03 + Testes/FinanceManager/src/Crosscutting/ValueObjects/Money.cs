using FinanceManager.BankAccounts;

namespace FinanceManager.Crosscutting.ValueObjects;

public readonly struct Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
        {
            return ValidationErrors.Money.AmountShouldBeGreaterThanZero;
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return ValidationErrors.Money.CurrencyShouldNotBeEmpty;
        }

        return new Money(amount, currency);
    }

    public static Money Default => new(0, "BRL");

    public static decimal operator /(decimal value, Money money)
    {
        return money.Amount / value;
    }

    public static Money operator +(Money first, Money second)
    {
        return new Money(second.Amount + first.Amount, first.Currency);
    }

    public static Money operator -(Money first, Money second)
    {
        return new Money(first.Amount - second.Amount, first.Currency);
    }

    public static implicit operator decimal(Money money)
    {
        return money.Amount;
    }
}
