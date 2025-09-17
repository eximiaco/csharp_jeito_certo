

namespace FinanceManager.Crosscutting.ValueObjects;

public readonly struct Percentage
{
    public decimal Value { get; init; }

    private Percentage(decimal value)
    {
        Value = value;
    }


    public static Result<Percentage> Create(decimal value)
    {
        if (value <= 0 || value >= 100)
        {
            return ValidationErrors.Percentage.InvalidValue;
        }

        return new Percentage(value);
    }

    public static bool operator >(decimal value, Percentage percentage)
    {
        return percentage.Value > value;
    }

    public static bool operator <(decimal value, Percentage percentage)
    {
        return percentage.Value < value;
    }

    public static implicit operator decimal(Percentage percentage)
    {
        return percentage.Value;
    }
}

